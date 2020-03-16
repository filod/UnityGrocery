using UnityEngine;
using Unity.Entities;

[ConverterVersion("filod", 1)]
public class HybridConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ParticleSystem particle, ParticleSystemRenderer renderer) =>
        {
            //var entity = GetPrimaryEntity(particle);
            Debug.Log($"convert particle!");
            AddHybridComponent(particle);
            AddHybridComponent(renderer);
            // add other common conversion here
        });
    }
}
