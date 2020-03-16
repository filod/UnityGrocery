using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Sample.Core;
using UnityEngine;


public class AbilityClimb
{
    public const Ability.AbilityTagValue Tag = Ability.AbilityTagValue.Climb;

    [Serializable]
    public struct Settings : IComponentData
    {
        public UserCommand.Button activateButton;
        public float Speed;
        public float Acceleration;
    }
    public struct PredictedState : IComponentData
    {

    }


    [UpdateInGroup(typeof(BehaviourRequestPhase))]
    [DisableAutoCreation]
    [AlwaysSynchronizeSystem]
    public class IdleStateUpdate : SystemBase
    {
        protected override void OnUpdate()
        {
            Dependency.Complete();

            var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
            Entities
                .ForEach((ref Ability.EnabledAbility activeAbility, ref Ability.AbilityStateIdle stateIdle, ref Settings settings) =>
                {
                    var command = playerControlledStateFromEntity[activeAbility.owner].command;
                    stateIdle.requestActive = activeAbility.activeButtonIndex == 0 && ClimbAllowed(in command);
                }).Run();
        }

        static bool ClimbAllowed(in UserCommand cmd)
        {
            //var sprintAllowed = cmd.moveMagnitude > 0 && (cmd.moveYaw < 90.0f || cmd.moveYaw > 270);
            return true;
        }

        [UpdateInGroup(typeof(MovementUpdatePhase))]
        [DisableAutoCreation]
        [AlwaysSynchronizeSystem]
        public class ActiveUpdate : SystemBase
        {
            protected override void OnUpdate()
            {
                Dependency.Complete();

                var playerControlledStateFromEntity = GetComponentDataFromEntity<PlayerControlled.State>(true);
                var characterVelocityFromEntity = GetComponentDataFromEntity<CharacterControllerVelocity>(false);
                var characterStartPositionFromEntity = GetComponentDataFromEntity<CharacterControllerMoveQuery>(false);
                var characterGroundDataFromEntity = GetComponentDataFromEntity<CharacterControllerGroundSupportData>(true);
                var characterPredictedDataFromEntity = GetComponentDataFromEntity<Character.PredictedData>(false);
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
                        var groundState = characterGroundDataFromEntity[activeAbility.owner];

                        var startPosition = characterStartPositionFromEntity[activeAbility.owner];
                        startPosition.StartPosition = charPredictedState.position;
                        characterStartPositionFromEntity[activeAbility.owner] = startPosition;

                        // 1. button hold + find closed climb position. else cancel climb
                        // 2. create surface constrain?
                        // vel = movVel + AvgSurfaceNormal + SurfaceVel?
                        var velocity = float3.zero;

                        var oldVel = characterVelocityFromEntity[activeAbility.owner];
                        oldVel.Velocity = velocity;
                        characterVelocityFromEntity[activeAbility.owner] = oldVel;


                    }).Run();
            }
        }

    }
}
