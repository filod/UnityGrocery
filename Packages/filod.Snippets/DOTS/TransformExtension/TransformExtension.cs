using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;


// This system updates all entities in the scene with LbLifetime component.
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class TransformUtilSystem : JobComponentSystem
{
    EntityCommandBufferSystem ecb;

    [BurstCompile]
    struct FollowJob : IJobForEach<Follow, Translation, LinearVelocity>
    {
        public float DeltaTime;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> followees;
        public void Execute(ref Follow follower,
                            [ReadOnly] ref Translation translation,
                            ref LinearVelocity velocity)
        {
            if (followees.Exists(follower.target))
            {
                // t * v = s
                //var dis = math.distance(translation.Value, followees[follower.target].Value);
                //if (Mathf.Approximately(dis, float.Epsilon)) return;
                //translation.Value = MathfxHelper.CurvedValueECS(
                //    AnimationCurveEnum.Hermite,
                //    translation.Value,
                //    followees[follower.target].Value,
                //    math.clamp(DeltaTime / (dis / 5.0f /*speed*/), 0.0f, 1.0f));
                var v = (Vector3)velocity.Value;
                Vector3.SmoothDamp(translation.Value, followees[follower.target].Value, ref v, 0.5f, Mathf.Infinity, DeltaTime);
                velocity.Value = v;
                //Debug.Log($"{follower.target} {followees[follower.target].Value}");
            }
            //Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            //pos = Pose + velecity * DeltaTime
        }
    }
    [BurstCompile]
    struct LookAtJob : IJobForEach<LookAt, Translation, LocalToWorld, Rotation>
    {
        public float DeltaTime;
        [ReadOnly]
        public ComponentDataFromEntity<Translation> targets;
        public void Execute(ref LookAt looker,
                            [ReadOnly] ref Translation translation,
                            ref LocalToWorld ltw,
                            ref Rotation rotation)
        {
            if (targets.Exists(looker.target))
            {
                var targetRot = quaternion.LookRotation(targets[looker.target].Value - translation.Value, math.up());
                rotation.Value = Quaternion.RotateTowards(rotation.Value, targetRot, DeltaTime * 180.0f);
            }
        }
    }

    struct AlignJob : IJobForEach<Align>
    {
        public float DeltaTime;
        public void Execute(ref Align align)
        {

        }
    }

    [BurstCompile]
    struct MoveJob : IJobForEach<LinearVelocity, Translation>
    {
        public float DeltaTime;
        public void Execute([ReadOnly] ref LinearVelocity lv, ref Translation t)
        {
            t.Value += DeltaTime * lv.Value;
        }
    }
    struct RotateJob : IJobForEach<AngularVelocity, Rotation>
    {
        public float DeltaTime;
        public void Execute([ReadOnly] ref AngularVelocity av, ref Rotation r)
        {
            r.Value = math.mul(r.Value, quaternion.Euler(DeltaTime * av.Value));
        }
    }

    protected override void OnCreate()
    {
        //ecb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDependencies)
    {

        var handle1 = new FollowJob
        {
            followees = GetComponentDataFromEntity<Translation>(true),
            DeltaTime = Time.DeltaTime
        }.Schedule(this, inputDependencies);

        var handle2 = new LookAtJob()
        {
            targets = GetComponentDataFromEntity<Translation>(true),
            DeltaTime = Time.DeltaTime
        }.Schedule(this, handle1);
        //
        var handle3 = new AlignJob()
        {
            DeltaTime = Time.DeltaTime
        }.Schedule(this, handle2);

        var handle4 = new MoveJob()
        {
            DeltaTime = Time.DeltaTime
        }.Schedule(this, handle3);

        var handle5 = new RotateJob()
        {
            DeltaTime = Time.DeltaTime
        }.Schedule(this, handle4);

        //var handle = JobHandle.CombineDependencies(
        //    new NativeArray<JobHandle>(new JobHandle[]{ handle1, handle2, handle3, handle4, handle5 }, 
        //    Allocator.TempJob));

        //ecb.AddJobHandleForProducer(handle);

        return handle5;
    }
}


