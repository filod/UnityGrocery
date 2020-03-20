using Unity.Entities;
using UnityEngine;

public class AbilityClimbAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityClimb.Settings settings = new AbilityClimb.Settings {
    };
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Climb });
        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, new AbilityClimb.PredictedState());
        //dstManager.AddComponentData(entity, new AbilityClimb.InterpolatedState());

#if UNITY_EDITOR
        dstManager.SetName(entity, name);
#endif
    }
}
