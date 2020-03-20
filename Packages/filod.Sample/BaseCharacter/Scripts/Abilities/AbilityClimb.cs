using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Sample.Core;
using UnityEngine;
using static CharacterControllerUtilities;


public class AbilityClimb
{
    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.Climb;

    public enum LocoState
    {
        Climb,
        ClimbMove
    }
    [Serializable]
    public struct Settings : IComponentData
    {
        public float Speed;
        public float Friction;
        public float Acceleration;
    }

    public struct PredictedState : IComponentData
    {
        [GhostDefaultField]
        public LocoState locoState;
        [GhostDefaultField(0)]
        public float3 SurfaceNormal;
        [GhostDefaultField(0)]
        public float3 SurfaceVelocity;
        [GhostDefaultField]
        public CharacterSupportState SupportedState;
    }

    static unsafe bool ClimbAllowed(
        in UserCommand command,
        in float3 pos,
        float rotation,
        in CharacterControllerComponentData ccData,
        in CharacterControllerCollider ccCollider,
        ref PhysicsWorld physicsWorld,
        float dt,
        out float3 avgSurfaceNormal,
        out float3 avgSurfaceVelocity
        )
    {
        var supported = false;
        avgSurfaceNormal = default;
        avgSurfaceVelocity = default;
        if (!command.buttons.IsSet(UserCommand.Button.SurfaceGrab))
        {
            return false;
        }


        var sampleAngleInvterval = 15f;
        var sampleCount = 7;
        for (int i = 0; i < sampleCount; i++)
        {
            float sign = i % 2 == 0 ? -1f : 1f;
            var targetAngle = rotation + sign * i * sampleAngleInvterval;
            var transform = new RigidTransform
            {
                pos = pos,
                rot = quaternion.identity
            };
            var forwardVec = math.forward(quaternion.Euler(0, math.radians(targetAngle), 0));
            var stepInput = new CharacterControllerStepInput
            {
                World = physicsWorld,
                DeltaTime = dt,
                Up = math.normalize(-forwardVec),
                SkinWidth = ccData.SkinWidth,
                ContactTolerance = ccData.ContactTolerance * 2.0f,
                MaxSlope = 90 - ccData.MaxSlope,
                RigidBodyIndex = -1,
                MaxMovementSpeed = ccData.MaxMovementSpeed
            };

            var collider = (Unity.Physics.Collider*)ccCollider.Collider.GetUnsafePtr();

            CheckSupport(
                ref physicsWorld,
                collider,
                stepInput,
                transform,
                out var supportedState,
                out var SurfaceNormal,
                out var SurfaceVelocity,
                out var constraints);
            if (supportedState == CharacterSupportState.Supported)
            {
                supported = true;
                avgSurfaceNormal += SurfaceNormal;
                avgSurfaceVelocity += SurfaceVelocity;
            }
            //var numSupportingPlanes = 0;
            //GameDebug.Log($"constraints.Length {constraints.Length} {}");
            //DebugDraw.Line(transform.pos, transform.pos + forwardVec, UnityEngine.Color.cyan);
            //for (int j = 0; j < constraints.Length; j++)
            //{
            //    var constraint = constraints[j];
            //    DebugDraw.Line(constraint.HitPosition, constraint.HitPosition + constraint.Plane.Normal, constraint.Touched && !constraint.IsTooSteep ? UnityEngine.Color.blue : UnityEngine.Color.red);
            //    DebugDraw.Sphere(constraint.HitPosition, 0.1f, constraint.IsTooSteep ? UnityEngine.Color.blue : UnityEngine.Color.red);
            //    DebugDraw.Circle(constraint.HitPosition, math.up(), 0.2f, constraint.Touched ? UnityEngine.Color.blue : UnityEngine.Color.red);
            //}
        }

        if (supported)
            return true;
        return false;

    }


    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class IdleStateUpdate : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency.Complete();

            var physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            var ccDataFromEntity = GetComponentDataFromEntity<CharacterControllerComponentData>(true);
            var ccColliderFromEntity = GetComponentDataFromEntity<CharacterControllerCollider>(true);
            var characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(false);
            var characterStartPositionFromEntity = GetComponentDataFromEntity<CharacterControllerMoveQuery>(false);
            var characterGroundDataFromEntity = GetComponentDataFromEntity<CharacterControllerGroundSupportData>(true);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false);
            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(true);
            var predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateIdle stateIdle, ref Settings settings) =>
                {
                    var command = playerControlledStateFromEntity[activeAbility.owner].command;
                    var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];
                    var charInterpolatedState = characterInterpolatedDataFromEntity[activeAbility.owner];
                    var groundState = characterGroundDataFromEntity[activeAbility.owner];
                    var ccData = ccDataFromEntity[activeAbility.owner];
                    var ccCollider = ccColliderFromEntity[activeAbility.owner];

                    var forwardVec = math.forward(quaternion.Euler(0, math.radians(charInterpolatedState.rotation), 0));

                    stateIdle.requestActive = activeAbility.activeButtonIndex == 0 && ClimbAllowed(
                        command, charPredictedState.position, charInterpolatedState.rotation, ccData, ccCollider, ref physicsWorld, time.tickDuration,
                        out var _,
                        out var __);
                    // reset velocity before climbing.
                    if (stateIdle.requestActive)
                    {
                        charPredictedState.velocity = float3.zero;
                        characterPredictedDataFromEntity[activeAbility.owner] = charPredictedState;
                    }
                }).Run();
        }
    }
    [UpdateInGroup(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class ActiveUpdate : SystemBase
    {
        protected unsafe override void OnUpdate()
        {
            Dependency.Complete();

            var physicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>().PhysicsWorld;
            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            var ccDataFromEntity = GetComponentDataFromEntity<CharacterControllerComponentData>(true);
            var ccColliderFromEntity = GetComponentDataFromEntity<CharacterControllerCollider>(true);
            var ccInitDataFromEntity = GetComponentDataFromEntity<CharacterControllerInitializationData>(true);
            var characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(false);
            var characterStartPositionFromEntity = GetComponentDataFromEntity<CharacterControllerMoveQuery>(false);
            var characterGroundDataFromEntity = GetComponentDataFromEntity<CharacterControllerGroundSupportData>(true);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false);
            var characterInterpolatedDataFromEntity = GetComponentDataFromEntity<Character.InterpolatedData>(false);
            var predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref PredictedState predictedState) =>
                {
                    if (!characterVelocityFromEntity.HasComponent(activeAbility.owner) &&
                        predictedFromEntity.Exists(activeAbility.owner) &&
                        !GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedFromEntity[activeAbility.owner]))
                        return;

                    var command = playerControlledStateFromEntity[activeAbility.owner].command;
                    var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];
                    var charInterpolatedState = characterInterpolatedDataFromEntity[activeAbility.owner];
                    var groundState = characterGroundDataFromEntity[activeAbility.owner];
                    var ccData = ccDataFromEntity[activeAbility.owner];
                    var ccInitData = ccInitDataFromEntity[activeAbility.owner];
                    var ccCollider = ccColliderFromEntity[activeAbility.owner];

                    var startPosition = characterStartPositionFromEntity[activeAbility.owner];
                    startPosition.StartPosition = charPredictedState.position;
                    startPosition.FollowGround = false;
                    startPosition.CheckSupport = false;

                    if (ClimbAllowed(
                        command, charPredictedState.position, charInterpolatedState.rotation, ccData, ccCollider, ref physicsWorld, time.tickDuration,
                        out var avgSurfaceNormal,
                        out var avgSurfaceVelocity))
                    {
                        var velocity = characterVelocityFromEntity[activeAbility.owner];

                        var newVelocity = charPredictedState.velocity;

                        newVelocity = (float3)(Quaternion.FromToRotation(avgSurfaceNormal, math.up()) * newVelocity);
                        //command.lookYaw = 0; //reset lookYaw
                        var planeVelocity = AbilityMovement.ActiveUpdate.CalculateGroundVelocity(newVelocity, avgSurfaceVelocity, ref command, true, settings.Speed, settings.Friction, settings.Acceleration, time.tickDuration);
                        newVelocity = (float3)(Quaternion.FromToRotation(math.up(), avgSurfaceNormal) * planeVelocity);
                        //DebugDraw.Line(charPredictedState.position, charPredictedState.position + avgSurfaceNormal, UnityEngine.Color.green);
                        var yAxis = new Vector3(0, 1, 0);

                        charInterpolatedState.rotation = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(-avgSurfaceNormal, yAxis), yAxis);

                        // follow wall
                        var vel = newVelocity;
                        if (math.lengthsq(vel) > 0f)
                        {
                            var up = math.normalize(avgSurfaceNormal);
                            var center = ccInitData.CapsuleHeight / 2;
                            var skinWidth = ccData.SkinWidth;
                            var startPos = startPosition.StartPosition - up * (skinWidth + ccInitData.CapsuleRadius) + new float3(0, center, 0);
                            var dir = math.normalizesafe(vel);
                            //var horizDir = new float3(dir.x, 0.0f, 0f);

                            var len = center; //use big circle as raycast
                            var endPos = startPos + len * dir;
                            var slopeAdjustment = up * len * math.tan(ccData.MaxSlope);
                            DebugDraw.Circle(startPos, up, len, Color.red);
                            var rayInput = new RaycastInput
                            {
                                Start = endPos + slopeAdjustment,
                                End = endPos - slopeAdjustment,
                                Filter = new CollisionFilter { BelongsTo = 1, CollidesWith = 1, GroupIndex = 0 }
                            };
                            var newDir = float3.zero;
                            var rayHit = new Unity.Physics.RaycastHit();
                            DebugDraw.Line(rayInput.Start, rayInput.End, Color.red);
                            if (physicsWorld.CastRay(rayInput, out rayHit))
                            {
                                newDir = math.normalize(rayHit.Position - startPos);
                                var newVel = newDir * math.length(vel);
                                DebugDraw.Line(startPosition.StartPosition, startPosition.StartPosition + newVel, Color.yellow);
                                newVelocity = newVel;
                                DebugDraw.Line(startPosition.StartPosition, startPosition.StartPosition + newVelocity, Color.blue);
                            }

                        }





                        velocity.Velocity = newVelocity;
                        characterStartPositionFromEntity[activeAbility.owner] = startPosition;
                        characterVelocityFromEntity[activeAbility.owner] = velocity;
                        characterPredictedDataFromEntity[activeAbility.owner] = charPredictedState;
                        characterInterpolatedDataFromEntity[activeAbility.owner] = charInterpolatedState;
                    } else
                    {
                        stateActive.requestCooldown = true;
                        return;
                    }
                }).Run();
        }
    }

    [UpdateInGroup(typeof(MovementResolvePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class HandleCollision : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false);
            var characterMoveResultFromEntity = GetComponentDataFromEntity<CharacterControllerMoveResult>(true);
            var characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(true);
            var predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref PredictedState predictedState) =>
                {
                    if (!characterMoveResultFromEntity.HasComponent(activeAbility.owner))
                        return;
                    if (predictedFromEntity.Exists(activeAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedFromEntity[activeAbility.owner]))
                        return;

                    var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];
                    var query = characterMoveResultFromEntity[activeAbility.owner];
                    var velocity = characterVelocityFromEntity[activeAbility.owner];


                    // Manually calculate resulting velocity as characterController.velocity is linked to Time.deltaTime
                    var newPos = query.MoveResult;
                    var newVelocity = velocity.Velocity;

                    charPredictedState.velocity = newVelocity;
                    charPredictedState.position = newPos;

                    characterPredictedDataFromEntity[activeAbility.owner] = charPredictedState;

                }).Run();

            return default;
        }
    }


    [UpdateInGroup(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class CooldownUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            Entities
                .ForEach((ref Settings settings, ref Ability.AbilityStateCooldown stateCooldown) =>
            {
                stateCooldown.requestIdle = true;
            }).Run();

            return default;
        }
    }
}
