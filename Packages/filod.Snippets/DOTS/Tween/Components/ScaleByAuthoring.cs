using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct ScaleBy : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Scale By
    [ReadOnly] public float3 TargetOffset;
}
public class ScaleByAuthoring : TweenAuthoringBase<ScaleBy>
{
    public float3 TargetOffset;
    internal override void ExtendTween(ref ScaleBy tween)
    {
        tween.TargetOffset = TargetOffset;
    }
}

