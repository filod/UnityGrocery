using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct RotateBy : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Rotate By
    [ReadOnly] public float3 TargetOffset;
}


public class RotateByAuthoring : TweenAuthoringBase<RotateBy>
{
    public float3 TargetOffset;
    internal override void ExtendTween(ref RotateBy tween)
    {
        tween.TargetOffset = TargetOffset;
    }
}
