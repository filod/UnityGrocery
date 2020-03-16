using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;


public struct MoveTo : IComponentData, ITweenSetting
{
    public float Delay { get; set; }
    public float Duration { get; set; }
    public bool LoopAnimation { get; set; }
    public AnimationCurveEnum AnimationCurve { get; set; }
    public float ElapsedTime { get; set; }

    // Move To
    [ReadOnly] public float3 StartValue;
    [ReadOnly] public float3 EndValue;
    [ReadOnly] public PathType PathType;
    [ReadOnly] public float3 TangentPoint0; // catmull-rom path only
    [ReadOnly] public float3 TangentPoint1;
}

public class MoveToAuthoring : TweenAuthoringBase<MoveTo>
{
    public float3 StartValue;
    public float3 EndValue;
    public PathType PathType;
    public float3 TangentPoint0;
    public float3 TangentPoint1;

    internal override void ExtendTween(ref MoveTo moveTo)
    {
        moveTo.StartValue = StartValue;
        moveTo.EndValue = EndValue;
        moveTo.PathType = PathType;
        moveTo.TangentPoint0 = TangentPoint0;
        moveTo.TangentPoint1 = TangentPoint1;
    }
}