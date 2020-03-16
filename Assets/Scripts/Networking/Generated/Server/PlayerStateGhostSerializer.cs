using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using Unity.Transforms;

public struct PlayerStateGhostSerializer : IGhostSerializer<PlayerStateSnapshotData>
{
    private ComponentType componentTypePlayerState;
    private ComponentType componentTypePlayerCharacterControlState;
    private ComponentType componentTypeLocalToWorld;
    private ComponentType componentTypeRotation;
    private ComponentType componentTypeTranslation;
    // FIXME: These disable safety since all serializers have an instance of the same type - causing aliasing. Should be fixed in a cleaner way
    [NativeDisableContainerSafetyRestriction][ReadOnly] private ArchetypeChunkComponentType<Player.State> ghostPlayerStateType;


    public int CalculateImportance(ArchetypeChunk chunk)
    {
        return 1;
    }

    public int SnapshotSize => UnsafeUtility.SizeOf<PlayerStateSnapshotData>();
    public void BeginSerialize(ComponentSystemBase system)
    {
        componentTypePlayerState = ComponentType.ReadWrite<Player.State>();
        componentTypePlayerCharacterControlState = ComponentType.ReadWrite<PlayerCharacterControl.State>();
        componentTypeLocalToWorld = ComponentType.ReadWrite<LocalToWorld>();
        componentTypeRotation = ComponentType.ReadWrite<Rotation>();
        componentTypeTranslation = ComponentType.ReadWrite<Translation>();
        ghostPlayerStateType = system.GetArchetypeChunkComponentType<Player.State>(true);
    }

    public void CopyToSnapshot(ArchetypeChunk chunk, int ent, uint tick, ref PlayerStateSnapshotData snapshot, GhostSerializerState serializerState)
    {
        snapshot.tick = tick;
        var chunkDataPlayerState = chunk.GetNativeArray(ghostPlayerStateType);
        snapshot.SetPlayerStateplayerId(chunkDataPlayerState[ent].playerId, serializerState);
        snapshot.SetPlayerStategameModeSystemInitialized(chunkDataPlayerState[ent].gameModeSystemInitialized, serializerState);
    }
}
