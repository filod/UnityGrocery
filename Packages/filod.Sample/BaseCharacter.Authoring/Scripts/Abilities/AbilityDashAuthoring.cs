using Unity.Entities;
using UnityEngine;

public class AbilityDashAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityDash.Settings settings = new AbilityDash.Settings
    {
        dashDistance = 5,
        dashDuration = 0.3f
    };
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Dash });
        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, new AbilityDash.PredictedState());

#if UNITY_EDITOR
        dstManager.SetName(entity, name);
#endif
    }
}
