using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using Unity.Transforms;

[GenerateAuthoringComponent]
public struct LifeTime : IComponentData
{
    public float Value;
}

public struct DestroyHierarchy : IComponentData
{
}

// This system updates all entities in the scene with LbLifetime component.
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class LifeTimeSystem : JobComponentSystem
{
    EntityCommandBufferSystem ecb;
    private NativeQueue<Entity> m_Queue;

    protected override void OnCreate()
    {
        ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_Queue = new NativeQueue<Entity>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        m_Queue.Dispose();
    }

    [BurstCompile]
    struct LifeTimeJob : IJobForEachWithEntity<LifeTime>
    {
        public float DeltaTime;
        public NativeQueue<Entity>.ParallelWriter Queue;

        public void Execute(Entity entity, int jobIndex, ref LifeTime lifeTime)
        {
            lifeTime.Value -= DeltaTime;
            if (lifeTime.Value < 0.0f)
            {
                Queue.Enqueue(entity);
            }
        }
    }

    struct LifeTimeCleanJob : IJob
    {
        public EntityCommandBuffer CommandBuffer;
        public NativeQueue<Entity> Queue;

        public void Execute()
        {
            while (Queue.Count > 0)
                CommandBuffer.AddComponent(Queue.Dequeue(), new DestroyHierarchy());
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {
        var handle = new LifeTimeJob
        {
            DeltaTime = Time.DeltaTime,
            Queue = m_Queue.AsParallelWriter()
        }.Schedule(this, inputDependencies);

        handle = new LifeTimeCleanJob()
        {
            Queue = m_Queue,
            CommandBuffer = ecb.CreateCommandBuffer()
        }.Schedule(handle);

        ecb.AddJobHandleForProducer(handle);

        return handle;
    }
}

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public class DestroyRootSystem : SystemBase
{

    EndSimulationEntityCommandBufferSystem m_endSimulationCmdBuffer;
    EntityQuery m_query;

    static void DestroyHierarchy(EntityCommandBuffer.Concurrent cmdBuffer, Entity entity, int index, BufferFromEntity<Child> childrenFromEntity)
    {
        if (!childrenFromEntity.Exists(entity))
        {
            cmdBuffer.DestroyEntity(index, entity);
            return;
        }
        var children = childrenFromEntity[entity];
        for (var i = 0; i < children.Length; ++i)
        {
            var childEntity = children[i].Value;
            cmdBuffer.DestroyEntity(index, childEntity);
            DestroyHierarchy(cmdBuffer, childEntity, index, childrenFromEntity);
        }
        cmdBuffer.DestroyEntity(index, entity);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        m_endSimulationCmdBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var CmdBuffer = m_endSimulationCmdBuffer.CreateCommandBuffer().ToConcurrent();
        var ChildrenFromEntity = GetBufferFromEntity<Child>(true);
        Dependency = Entities
            .WithReadOnly(ChildrenFromEntity)
            .WithStoreEntityQueryInField(ref m_query)
            .WithAll<DestroyHierarchy>()
            .ForEach((Entity entity, int entityInQueryIndex) => {
            DestroyHierarchy(CmdBuffer, entity, entityInQueryIndex, ChildrenFromEntity);
        }).Schedule(Dependency);
        m_endSimulationCmdBuffer.CreateCommandBuffer().RemoveComponent(m_query, typeof(DestroyHierarchy));
        m_endSimulationCmdBuffer.AddJobHandleForProducer(Dependency);
    }
}

