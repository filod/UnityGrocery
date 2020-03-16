using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using System;
using Unity.Burst;

//<todo.eoin.usermod Rename to ModifyOverlappingBodyPairsComponentData?
public struct DisableCollision : IComponentData { }

// A system which configures the simulation step to disable certain broad phase pairs
[UpdateBefore(typeof(StepPhysicsWorld))]
public class DisableBroadphasePairsSystem : JobComponentSystem
{
    EntityQuery m_PairModifierGroup;

    BuildPhysicsWorld m_PhysicsWorld;
    StepPhysicsWorld m_StepPhysicsWorld;

    protected override void OnCreate()
    {
        m_PhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

        m_PairModifierGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(DisableCollision) }
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (m_PairModifierGroup.CalculateEntityCount() == 0)
        {
            return inputDeps;
        }

        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics)
        {
            return inputDeps;
        }

        // Add a custom callback to the simulation, which will inject our custom job after the body pairs have been created
        SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            inDeps.Complete(); //<todo Needed to initialize our modifier

            return new DisablePairsJob
            {
                Bodies = m_PhysicsWorld.PhysicsWorld.Bodies,
                DisableGroup = GetComponentDataFromEntity<DisableCollision>(true)
            }.Schedule(simulation, ref world, inputDeps);
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, callback);

        return inputDeps;
    }

    [BurstCompile]
    struct DisablePairsJob : IBodyPairsJob
    {
        [ReadOnly] public NativeSlice<RigidBody> Bodies;
        [ReadOnly] public ComponentDataFromEntity<DisableCollision> DisableGroup;

        public unsafe void Execute(ref ModifiableBodyPair pair)
        {
            // Disable the pair if a box collides with a static object
            int indexA = pair.BodyIndices.BodyAIndex;
            int indexB = pair.BodyIndices.BodyBIndex;
            if (DisableGroup.HasComponent(Bodies[indexA].Entity) || DisableGroup.HasComponent(Bodies[indexB].Entity))
            {
                pair.Disable();
            }
        }
    }
}
