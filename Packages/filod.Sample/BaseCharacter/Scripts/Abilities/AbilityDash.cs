using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;


public class AbilityDash
{
    public enum LocoState
    {
        DashStart,
        Dash,
        DashEnd,
        MaxValue
    }
    [Serializable]
    public struct Settings : IComponentData
    {
        public float coolDownDuration;
        public float dashDuration;
        public float dashDistance;
    }
    public struct PredictedState : IComponentData
    {
        [GhostDefaultField]
        public LocoState locoState;
        [GhostDefaultField]
        public int locoStartTick;
        [GhostDefaultField(0)]
        public float3 startVelocity;
    }

    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class IdleUpdate : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete();

            var healthStateFromEntity = GetComponentDataFromEntity<HealthStateData>(true);
            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            var predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities
                .ForEach((ref Ability.AbilityStateIdle stateIdle, ref Ability.EnabledAbility enabledAbility, ref PredictedState predictedState) =>
                {
                    if (predictedFromEntity.Exists(enabledAbility.owner) && !GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedFromEntity[enabledAbility.owner]))
                        return;

                    var shouldDash = enabledAbility.activeButtonIndex == 0 && DashAllowed(healthStateFromEntity[enabledAbility.owner]);
                    if (shouldDash)
                    {
                        stateIdle.requestActive = true;
                        predictedState.locoStartTick = time.tick;
                    }
                }).Run();

            return default;
        }

        static private bool DashAllowed(in HealthStateData healthState)
        {
            // TODO (mogensh) hack to disable input when dead. Ability should not be running, but as this is only ability that handles move request and gravity we keep it running and disable unput
            return healthState.health > 0;
        }
    }

    [UpdateInGroup(typeof(MovementUpdatePhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class ActiveUpdate : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency.Complete();

            var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false);
            var characterStartPositionFromEntity = GetComponentDataFromEntity<CharacterControllerMoveQuery>(false);
            var characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(false);
            var predictedFromEntity = GetComponentDataFromEntity<PredictedGhostComponent>(false);
            var PredictingTick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;

            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateActive stateActive, ref Settings settings, ref PredictedState predictedState) =>
                {
                    if (!characterVelocityFromEntity.HasComponent(activeAbility.owner) &&
                        predictedFromEntity.Exists(activeAbility.owner) &&
                        !GhostPredictionSystemGroup.ShouldPredict(PredictingTick, predictedFromEntity[activeAbility.owner]))
                        return;


                    var command = playerControlledStateFromEntity[activeAbility.owner].command;
                    var charPredictedState = characterPredictedDataFromEntity[activeAbility.owner];

                    var startPosition = characterStartPositionFromEntity[activeAbility.owner];
                    startPosition.StartPosition = charPredictedState.position;
                    characterStartPositionFromEntity[activeAbility.owner] = startPosition;

                    var phaseDuration = time.DurationSinceTick(predictedState.locoStartTick);

                    if (phaseDuration >= settings.dashDuration)
                    {
                        stateActive.requestCooldown = true;
                        return;
                    }
                    //GameDebug.Log($"{predictedState.locoStartTick} :: {time.tick}");
                    var velocity = predictedState.startVelocity;
                    if (predictedState.locoStartTick + 1 == time.tick)
                    {
                        var dashVel = settings.dashDistance / settings.dashDuration;
                        var moveYawRotation = Quaternion.Euler(0, command.lookYaw + command.moveYaw, 0);
                        var moveVec = moveYawRotation * Vector3.forward;
                        velocity = moveVec * dashVel;
                        velocity.y = 0f;
                        predictedState.startVelocity = velocity;
                        //GameDebug.Log($"predictedState.startVelocity {predictedState.startVelocity}");
                    }

                    var oldVel = characterVelocityFromEntity[activeAbility.owner];
                    oldVel.Velocity = velocity;
                    characterVelocityFromEntity[activeAbility.owner] = oldVel;

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
