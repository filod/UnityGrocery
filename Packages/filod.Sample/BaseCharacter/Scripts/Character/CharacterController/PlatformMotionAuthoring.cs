using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics.Systems;
using Random = Unity.Mathematics.Random;
using Unity.Sample.Core;

public struct PlatformMotion : IComponentData
{
    public float CurrentTime;
    public float3 InitialPosition;
    public float3 DesiredPosition;
    public float Height;
    public float Speed;
    public float3 Direction;
    public float3 Rotation;
}

public class PlatformMotionAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Height = 1f;
    public float Speed = 1f;
    public float3 Direction = math.up();
    public float3 Rotation = float3.zero;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData<PlatformMotion>(entity, new PlatformMotion
        {
            InitialPosition = transform.position,
            DesiredPosition = transform.position,
            Height = Height,
            Speed = Speed,
            Direction = math.normalizesafe(Direction),
            Rotation = Rotation,
        });
    }
}
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class PlatformMotionSystem : SystemBase
{
    protected override void OnCreate()
    {
    }
    protected override void OnUpdate()
    {
        var time = GetEntityQuery(ComponentType.ReadOnly<GlobalGameTime>()).GetSingleton<GlobalGameTime>().gameTime;
        Entities.ForEach((ref PlatformMotion motion, ref PhysicsVelocity velocity, in Translation position) =>
        {
            motion.CurrentTime += time.tickDuration;

            var desiredOffset = motion.Height * math.sin(motion.CurrentTime * motion.Speed);
            var currentOffset = math.dot(position.Value - motion.InitialPosition, motion.Direction);
            velocity.Linear = motion.Direction * (desiredOffset - currentOffset);

            velocity.Angular = motion.Rotation;

            //GameDebug.Log($"1 groundState.SurfaceVelocity {velocity.Linear.z}");
        }).Schedule(Dependency);
    }
}
