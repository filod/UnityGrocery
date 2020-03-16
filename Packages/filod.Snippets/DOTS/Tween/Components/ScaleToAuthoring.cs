using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct ScaleTo : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Scale To
    [ReadOnly] public float3 StartValue;
    [ReadOnly] public float3 EndValue;
}


public class ScaleToAuthoring : TweenAuthoringBase<ScaleTo>
{
    public float3 StartValue;
    public float3 EndValue;
    internal override void ExtendTween(ref ScaleTo tween)
    {
        tween.StartValue = StartValue;
        tween.EndValue = EndValue;
    }
}

