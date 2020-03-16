using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct Hover : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Hover
    [ReadOnly] public float3 StartValue;
    [ReadOnly] public float3 Offset;
}

public class HoverAuthoring : TweenAuthoringBase<Hover>
{
    public float3 Offset;

    internal override void ExtendTween(ref Hover moveTo)
    {
        moveTo.StartValue = transform.position;
        moveTo.Offset = Offset;
    }
}