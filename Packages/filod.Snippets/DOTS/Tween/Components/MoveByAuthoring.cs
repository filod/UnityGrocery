using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct MoveBy : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Move By
    [ReadOnly] public float3 TargetOffset;
}


public class MoveByAuthoring : TweenAuthoringBase<MoveBy>
{
    public float3 TargetOffset;
    internal override void ExtendTween(ref MoveBy tween)
    {
        tween.TargetOffset = TargetOffset;
    }
}
