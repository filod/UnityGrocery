using UnityEngine;
using System.Collections;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct Align : IComponentData
{
    public Entity target;
}