using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
public struct RotateTo : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Rotate To
    [ReadOnly] public float3 StartValue;
    [ReadOnly] public float3 EndValue;
}


public class RotateToAuthoring : TweenAuthoringBase<RotateTo>
{
    [ReadOnly] public float3 StartValue;
    [ReadOnly] public float3 EndValue;
    internal override void ExtendTween(ref RotateTo tween)
    {
        tween.StartValue = StartValue;
        tween.EndValue = EndValue;
    }
}
