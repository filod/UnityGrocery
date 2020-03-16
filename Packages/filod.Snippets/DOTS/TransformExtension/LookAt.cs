using UnityEngine;
using System.Collections;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct LookAt : IComponentData
{
    public Entity target;
}
