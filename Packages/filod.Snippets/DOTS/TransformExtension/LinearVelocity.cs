using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct LinearVelocity : IComponentData
{
    public float3 Value;
}