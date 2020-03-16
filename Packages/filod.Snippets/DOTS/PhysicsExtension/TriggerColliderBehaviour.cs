using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using System;
using UnityEditor;



public struct CollidingTriggerCollider : IBufferElementData
{
    public Entity TriggerColliderEntity;
    //public int CreatedFrame;
    //public int CurrentFrame;

    //public bool HasJustEntered { get { return (CurrentFrame - CreatedFrame) == 0; } }
}

public struct TriggerCollider : IComponentData
{
    //public int CurrentFrame;
}

[AlwaysUpdateSystem]
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class TriggerColliderSystem : JobComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;
    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    //[BurstCompile]
    struct CollisionEventStopAnimationJob : ICollisionEventsJob
    {
        public EntityCommandBuffer CommandBuffer;
        [ReadOnly] public ComponentDataFromEntity<TriggerCollider> TriggerColliderGroup;
        public BufferFromEntity<CollidingTriggerCollider> CollidingTriggerColliderGroup;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;


            bool isBodyATriggerCollider = TriggerColliderGroup.Exists(entityA);
            bool isBodyBTriggerCollider = TriggerColliderGroup.Exists(entityB);
            if (isBodyATriggerCollider) SetCollidingTriggerCollider(entityA, entityB);
            if (isBodyBTriggerCollider) SetCollidingTriggerCollider(entityB, entityA);
        }

        private void SetCollidingTriggerCollider(Entity triggerCollider, Entity other)
        {
            //Debug.Log("meh!");
            DynamicBuffer<CollidingTriggerCollider> buf;
            if (!CollidingTriggerColliderGroup.Exists(other))
            {
                buf = CommandBuffer.AddBuffer<CollidingTriggerCollider>(other);
            }
            else
            {
                buf = CollidingTriggerColliderGroup[other];
            }
            buf.Add(new CollidingTriggerCollider { TriggerColliderEntity = triggerCollider });
        }
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // clear all buffer at first
        Entities.ForEach((ref DynamicBuffer<CollidingTriggerCollider> buf) => {
            buf.Clear();
        }).Run();

        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);

        JobHandle jobHandle = new CollisionEventStopAnimationJob
        {
            CommandBuffer = PostUpdateCommands,
            TriggerColliderGroup = GetComponentDataFromEntity<TriggerCollider>(true),
            CollidingTriggerColliderGroup = GetBufferFromEntity<CollidingTriggerCollider>()
        }.Schedule(m_StepPhysicsWorldSystem.Simulation,
                    ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        jobHandle.Complete();

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();


        return jobHandle;
    }
}