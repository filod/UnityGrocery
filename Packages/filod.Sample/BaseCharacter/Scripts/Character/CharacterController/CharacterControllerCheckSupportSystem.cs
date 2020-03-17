﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;

[UpdateInGroup(typeof(AbilityUpdateSystemGroup))]
[UpdateBefore(typeof(MovementUpdatePhase))]
[DisableAutoCreation]
[AlwaysSynchronizeSystem]
[AlwaysUpdateSystem]
public class CharacterControllerCheckSupportSystem : JobComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    EntityQuery m_GameTimeSingletonQuery;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_GameTimeSingletonQuery = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>());
    }

    protected unsafe override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var physicsWorld = m_BuildPhysicsWorldSystem.PhysicsWorld;
        var time = m_GameTimeSingletonQuery.GetSingleton<GlobalGameTime>().gameTime;
        var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

        Entities
            .WithName("CheckSupportJob")
            .ForEach((
                    ref CharacterControllerComponentData ccData,
                    ref CharacterControllerMoveQuery ccQuery,
                    ref CharacterControllerMoveResult resultPosition,
                    ref CharacterControllerVelocity velocity,
                    ref CharacterControllerCollider ccCollider,
                    ref CharacterControllerGroundSupportData ccGroundData,
                    in PredictedGhostComponent predictedGhostComponent) =>
                {
                    if (!GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedGhostComponent))
                        return;

                    if (!ccQuery.CheckSupport)
                    {
                        ccGroundData.SupportedState = CharacterControllerUtilities.CharacterSupportState.Unsupported;
                        ccGroundData.SurfaceVelocity = float3.zero;
                        ccGroundData.SurfaceNormal = float3.zero;
                        return;
                    }

                    var stepInput = new CharacterControllerUtilities.CharacterControllerStepInput
                    {
                        World = physicsWorld,
                        DeltaTime = time.tickDuration,
                        Up = math.up(),
                        Gravity = new float3(0.0f, -9.8f, 0.0f),
                        MaxIterations = ccData.MaxIterations,
                        Tau = 0.4f, // CharacterControllerUtilities.k_DefaultTau,
                        Damping = 0.9f, // CharacterControllerUtilities.k_DefaultDamping,
                        SkinWidth = ccData.SkinWidth,
                        ContactTolerance = ccData.ContactTolerance * 2.0f,
                        MaxSlope = ccData.MaxSlope,
                        RigidBodyIndex = -1,
                        CurrentVelocity = velocity.Velocity,
                        MaxMovementSpeed = ccData.MaxMovementSpeed
                    };

                    var transform = new RigidTransform
                    {
                        pos = resultPosition.MoveResult,
                        rot = quaternion.identity
                    };

                    var collider = (Unity.Physics.Collider*)ccCollider.Collider.GetUnsafePtr();

                    // FollowGround can cause the collider to lift further above ground
                    // before entering upwards slopes or exiting downwards slopes.
                    // Halfpipes show the issue the most.
                    // Lengthen the ground probe vector to remove undesired unsupporteds.
                    float probeFactor = ccQuery.FollowGround ? 2 : 1;
                    // Check support
                    CharacterControllerUtilities.CheckSupport(
                        ref physicsWorld,
                        collider,
                        stepInput,
                        transform,
                        out ccGroundData.SupportedState,
                        out ccGroundData.SurfaceNormal,
                        out ccGroundData.SurfaceVelocity);

                    //Unity.Sample.Core.GameDebug.Log($"2 groundState.SurfaceVelocity {ccGroundData.SurfaceVelocity}");
                }).Run();


        return inputDeps;
    }
}
