using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct AngularVelocity : IComponentData
{
    public float3 Value;
}
