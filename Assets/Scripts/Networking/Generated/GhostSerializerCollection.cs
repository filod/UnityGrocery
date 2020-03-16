using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct UnityGroceryGhostSerializerCollection : IGhostSerializerCollection
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string[] CreateSerializerNameList()
    {
        var arr = new string[]
        {
            "Char_CapsuleGhostSerializer",
            "PlayerStateGhostSerializer",
            "GameModeGhostSerializer",
            "platformGhostSerializer",
        };
        return arr;
    }

    public int Length => 4;
#endif
    public static int FindGhostType<T>()
        where T : struct, ISnapshotData<T>
    {
        if (typeof(T) == typeof(Char_CapsuleSnapshotData))
            return 0;
        if (typeof(T) == typeof(PlayerStateSnapshotData))
            return 1;
        if (typeof(T) == typeof(GameModeSnapshotData))
            return 2;
        if (typeof(T) == typeof(platformSnapshotData))
            return 3;
        return -1;
    }

    public void BeginSerialize(ComponentSystemBase system)
    {
        m_Char_CapsuleGhostSerializer.BeginSerialize(system);
        m_PlayerStateGhostSerializer.BeginSerialize(system);
        m_GameModeGhostSerializer.BeginSerialize(system);
        m_platformGhostSerializer.BeginSerialize(system);
    }

    public int CalculateImportance(int serializer, ArchetypeChunk chunk)
    {
        switch (serializer)
        {
            case 0:
                return m_Char_CapsuleGhostSerializer.CalculateImportance(chunk);
            case 1:
                return m_PlayerStateGhostSerializer.CalculateImportance(chunk);
            case 2:
                return m_GameModeGhostSerializer.CalculateImportance(chunk);
            case 3:
                return m_platformGhostSerializer.CalculateImportance(chunk);
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int GetSnapshotSize(int serializer)
    {
        switch (serializer)
        {
            case 0:
                return m_Char_CapsuleGhostSerializer.SnapshotSize;
            case 1:
                return m_PlayerStateGhostSerializer.SnapshotSize;
            case 2:
                return m_GameModeGhostSerializer.SnapshotSize;
            case 3:
                return m_platformGhostSerializer.SnapshotSize;
        }

        throw new ArgumentException("Invalid serializer type");
    }

    public int Serialize(ref DataStreamWriter dataStream, SerializeData data)
    {
        switch (data.ghostType)
        {
            case 0:
            {
                return GhostSendSystem<UnityGroceryGhostSerializerCollection>.InvokeSerialize<Char_CapsuleGhostSerializer, Char_CapsuleSnapshotData>(m_Char_CapsuleGhostSerializer, ref dataStream, data);
            }
            case 1:
            {
                return GhostSendSystem<UnityGroceryGhostSerializerCollection>.InvokeSerialize<PlayerStateGhostSerializer, PlayerStateSnapshotData>(m_PlayerStateGhostSerializer, ref dataStream, data);
            }
            case 2:
            {
                return GhostSendSystem<UnityGroceryGhostSerializerCollection>.InvokeSerialize<GameModeGhostSerializer, GameModeSnapshotData>(m_GameModeGhostSerializer, ref dataStream, data);
            }
            case 3:
            {
                return GhostSendSystem<UnityGroceryGhostSerializerCollection>.InvokeSerialize<platformGhostSerializer, platformSnapshotData>(m_platformGhostSerializer, ref dataStream, data);
            }
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    private Char_CapsuleGhostSerializer m_Char_CapsuleGhostSerializer;
    private PlayerStateGhostSerializer m_PlayerStateGhostSerializer;
    private GameModeGhostSerializer m_GameModeGhostSerializer;
    private platformGhostSerializer m_platformGhostSerializer;
}

public struct EnableUnityGroceryGhostSendSystemComponent : IComponentData
{}
public class UnityGroceryGhostSendSystem : GhostSendSystem<UnityGroceryGhostSerializerCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<EnableUnityGroceryGhostSendSystemComponent>();
    }
}
