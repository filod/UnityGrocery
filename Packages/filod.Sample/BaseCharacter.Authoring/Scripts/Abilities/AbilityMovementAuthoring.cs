using Unity.Entities;
using UnityEngine;

[ConverterVersion("filod", 20)]
public class AbilityMovementAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public AbilityMovement.Settings settings = new AbilityMovement.Settings {
        playerSpeed = 5,
        playerSprintSpeed = 7,
        playerAcceleration = 15,
        playerFriction = 12,
        playerAiracceleration = 5,
        playerAirFriction = 1,
        playerGravity = 18,
        easterBunny = true,
        jumpAscentVelocity = 10,
        maxFallVelocity = 50,
        doubleJumpAllowed = true,
        fallMutiplier = 2.5f,
        lowJumpMutiplier = 2,
    };
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new Ability.AbilityTag { Value = Ability.AbilityTagValue.Movement });
        dstManager.AddComponentData(entity, settings);
        dstManager.AddComponentData(entity, new AbilityMovement.PredictedState());
        dstManager.AddComponentData(entity, new AbilityMovement.InterpolatedState());

#if UNITY_EDITOR
        dstManager.SetName(entity,name);
#endif
    }
}
