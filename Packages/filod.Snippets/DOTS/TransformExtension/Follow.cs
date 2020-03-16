using Unity.Entities;

[GenerateAuthoringComponent]
public struct Follow : IComponentData
{
    public Entity target;
}

