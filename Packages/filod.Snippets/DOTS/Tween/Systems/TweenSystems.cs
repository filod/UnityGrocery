using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;


public interface ITweenSetting
{
    float Delay { get; set; } // Animation Delay.
    float Duration { get; set; } // Animation Duration.
    bool LoopAnimation { get; set; } // Is animation looping?
    AnimationCurveEnum AnimationCurve { get; set; } // Animation curve used for this tween.
    float ElapsedTime { get; set; } // Animation Elapsed Time.
}

public abstract class TweenAuthoringBase<T> : MonoBehaviour, IConvertGameObjectToEntity where T : struct, IComponentData, ITweenSetting
{
    public float Delay;
    public float Duration;
    public bool LoopAnimation;
    public AnimationCurveEnum AnimationCurve;
    public float ElapsedTime;
    public T GetTween()
    {
        return new T()
        {
            Delay = Delay,
            Duration = Duration,
            LoopAnimation = LoopAnimation,
            AnimationCurve = AnimationCurve,
            ElapsedTime = ElapsedTime,
        };
    }
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {

        var tween = GetTween();
        ExtendTween(ref tween);
        dstManager.AddComponentData(entity, tween);
    }

    internal abstract void ExtendTween(ref T tween);
}

public abstract class TweenSystemBase<TTween, TTarget> : ComponentSystem where TTween : struct, IComponentData, ITweenSetting where TTarget : struct, IComponentData
{
    protected virtual void OnTweenEnd(Entity entity, ref TTween tween, ref TTarget target) { }
    protected virtual void OnTweenProcess(Entity entity, ref TTween tween, ref TTarget target, ref float t, ref float t1) { }
    protected override void OnUpdate()
    {
        // TODO: jobify this
        //var cbSys = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        //var cb = cbSys.CreateCommandBuffer().ToConcurrent();
        var deltaTime = Time.DeltaTime;
        Entities.ForEach((Entity entity, ref TTween tween, ref TTarget target) =>
        {
            tween.ElapsedTime += deltaTime;
            var t = math.clamp((tween.ElapsedTime - tween.Delay) / tween.Duration, 0.0f, 1.0f);
            var t1 = math.clamp((tween.ElapsedTime - tween.Delay + deltaTime) / tween.Duration, 0.0f, 1.0f);
            OnTweenProcess(entity, ref tween, ref target, ref t, ref t1);


            if (tween.ElapsedTime >= tween.Delay + tween.Duration)
            {
                if (tween.LoopAnimation)
                {
                    tween.ElapsedTime -= tween.Duration;
                }
                else
                {
                    OnTweenEnd(entity, ref tween, ref target);
                    EntityManager.RemoveComponent<TTween>(entity);
                }
            }
        });
        //    .Schedule(inputDeps);
        //cbSys.AddJobHandleForProducer(inputDeps);
        //return inputDeps;
    }
}

public class HoverSystem : TweenSystemBase<Hover, Translation>
{
    protected override void OnTweenProcess(Entity entity, ref Hover hover, ref Translation translation, ref float t, ref float t1)
    {
        translation.Value = MathfxHelper.CurvedValueECS(hover.AnimationCurve, hover.StartValue, hover.StartValue + hover.Offset, (-math.abs(t - 0.5f) + 0.5f) * 2);
        //translation.Value = MathfxHelper.CurvedValueECS(hover.AnimationCurve, hover.BottomValue, hover.TopValue, (-math.abs(t - 0.5f) + 0.5f) * 2);
    }
    protected override void OnTweenEnd(Entity entity, ref Hover tween, ref Translation target)
    {
        target.Value = tween.StartValue;
    }
}

public class MoveBySystem : TweenSystemBase<MoveBy, Translation>
{
    protected override void OnTweenProcess(Entity entity, ref MoveBy moveBy, ref Translation translation, ref float t, ref float t1)
    {
        var start = MathfxHelper.CurvedValueECS(moveBy.AnimationCurve, float3.zero, moveBy.TargetOffset, t);
        var end = MathfxHelper.CurvedValueECS(moveBy.AnimationCurve, float3.zero, moveBy.TargetOffset, t1);

        translation.Value += end - start;
    }
}

public class MoveToSystem : TweenSystemBase<MoveTo, Translation>
{
    protected override void OnTweenProcess(Entity entity, ref MoveTo tween, ref Translation target, ref float t, ref float t1)
    {

        if (tween.PathType == PathType.CatmullRom)
        {
            target.Value = CatmullRom.CalculatePosition(tween.StartValue, tween.EndValue, tween.TangentPoint0, tween.TangentPoint1,
                MathfxHelper.CurvedValueECS(tween.AnimationCurve, 0, 1, t));
        }
        else
        {
            target.Value = MathfxHelper.CurvedValueECS(tween.AnimationCurve, tween.StartValue, tween.EndValue, t);
        }
    }
    protected override void OnTweenEnd(Entity entity, ref MoveTo tween, ref Translation target)
    {
        target.Value = tween.EndValue;
    }
}

public class RotateBySystem : TweenSystemBase<RotateBy, Rotation>
{
    protected override void OnTweenProcess(Entity entity, ref RotateBy RotateBy, ref Rotation rotation, ref float t, ref float t1)
    {
        var start = MathfxHelper.CurvedValueECS(RotateBy.AnimationCurve, float3.zero, RotateBy.TargetOffset, t);
        var end = MathfxHelper.CurvedValueECS(RotateBy.AnimationCurve, float3.zero, RotateBy.TargetOffset, t1);

        rotation.Value = math.mul(math.normalize(rotation.Value), quaternion.Euler(end - start));
    }
}

public class RotateToSystem : TweenSystemBase<RotateTo, Rotation>
{
    protected override void OnTweenProcess(Entity entity, ref RotateTo rotateTo, ref Rotation rotation, ref float t, ref float t1)
    {
        var eulerRot = MathfxHelper.CurvedValueECS(rotateTo.AnimationCurve, rotateTo.StartValue, rotateTo.EndValue, t);
        rotation.Value = quaternion.Euler(eulerRot);
    }
    protected override void OnTweenEnd(Entity entity, ref RotateTo rotateTo, ref Rotation rotation)
    {
        rotation.Value = quaternion.Euler(rotateTo.EndValue);
    }
}

public class ScaleBySystem : TweenSystemBase<ScaleBy, NonUniformScale>
{
    protected override void OnTweenProcess(Entity entity, ref ScaleBy scaleBy, ref NonUniformScale scale, ref float t, ref float t1)
    {
        var start = MathfxHelper.CurvedValueECS(scaleBy.AnimationCurve, float3.zero, scaleBy.TargetOffset, t);
        var end = MathfxHelper.CurvedValueECS(scaleBy.AnimationCurve, float3.zero, scaleBy.TargetOffset, t1);

        scale.Value += end - start;
    }
}

public class ScaleToSystem : TweenSystemBase<ScaleTo, NonUniformScale>
{
    protected override void OnTweenProcess(Entity entity, ref ScaleTo scaleTo, ref NonUniformScale scale, ref float t, ref float t1)
    {
        scale.Value = MathfxHelper.CurvedValueECS(scaleTo.AnimationCurve, scaleTo.StartValue, scaleTo.EndValue, t);
    }
    protected override void OnTweenEnd(Entity entity, ref ScaleTo scaleTo, ref NonUniformScale scale)
    {
        scale.Value = scaleTo.EndValue;
    }
}

public struct MoveToward : IComponentData, ITweenSetting {

    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }


    public Entity startAnchor;
    public Entity endAnchor;
}

public class MoveTowardSystem : TweenSystemBase<MoveToward, Translation>
{
    protected override void OnTweenProcess(Entity entity, ref MoveToward tween, ref Translation translation, ref float t, ref float t1)
    {
        var start = EntityManager.GetComponentData<LocalToWorld>(tween.startAnchor).Position;
        var end = EntityManager.GetComponentData<LocalToWorld>(tween.endAnchor).Position;
        translation.Value = MathfxHelper.CurvedValueECS(tween.AnimationCurve, start, end, t);
        //Vector3.MoveTowards()
        //scale.Value = MathfxHelper.CurvedValueECS(scaleTo.AnimationCurve, scaleTo.StartValue, scaleTo.EndValue, t);
    }
    protected override void OnTweenEnd(Entity entity, ref MoveToward tween, ref Translation target)
    {
        var end = EntityManager.GetComponentData<LocalToWorld>(tween.endAnchor).Position;
        target.Value = end;
    }
}
