using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;

public struct UnityGroceryGhostDeserializerCollection : IGhostDeserializerCollection
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string[] CreateSerializerNameList()
    {
        var arr = new string[]
        {
            "Char_CapsuleGhostSerializer",
            "PlayerStateGhostSerializer",
            "GameModeGhostSerializer",
            "Char_BanditGhostSerializer",
        };
        return arr;
    }

    public int Length => 4;
#endif
    public void Initialize(World world)
    {
        var curChar_CapsuleGhostSpawnSystem = world.GetOrCreateSystem<Char_CapsuleGhostSpawnSystem>();
        m_Char_CapsuleSnapshotDataNewGhostIds = curChar_CapsuleGhostSpawnSystem.NewGhostIds;
        m_Char_CapsuleSnapshotDataNewGhosts = curChar_CapsuleGhostSpawnSystem.NewGhosts;
        curChar_CapsuleGhostSpawnSystem.GhostType = 0;
        var curPlayerStateGhostSpawnSystem = world.GetOrCreateSystem<PlayerStateGhostSpawnSystem>();
        m_PlayerStateSnapshotDataNewGhostIds = curPlayerStateGhostSpawnSystem.NewGhostIds;
        m_PlayerStateSnapshotDataNewGhosts = curPlayerStateGhostSpawnSystem.NewGhosts;
        curPlayerStateGhostSpawnSystem.GhostType = 1;
        var curGameModeGhostSpawnSystem = world.GetOrCreateSystem<GameModeGhostSpawnSystem>();
        m_GameModeSnapshotDataNewGhostIds = curGameModeGhostSpawnSystem.NewGhostIds;
        m_GameModeSnapshotDataNewGhosts = curGameModeGhostSpawnSystem.NewGhosts;
        curGameModeGhostSpawnSystem.GhostType = 2;
        var curChar_BanditGhostSpawnSystem = world.GetOrCreateSystem<Char_BanditGhostSpawnSystem>();
        m_Char_BanditSnapshotDataNewGhostIds = curChar_BanditGhostSpawnSystem.NewGhostIds;
        m_Char_BanditSnapshotDataNewGhosts = curChar_BanditGhostSpawnSystem.NewGhosts;
        curChar_BanditGhostSpawnSystem.GhostType = 3;
    }

    public void BeginDeserialize(JobComponentSystem system)
    {
        m_Char_CapsuleSnapshotDataFromEntity = system.GetBufferFromEntity<Char_CapsuleSnapshotData>();
        m_PlayerStateSnapshotDataFromEntity = system.GetBufferFromEntity<PlayerStateSnapshotData>();
        m_GameModeSnapshotDataFromEntity = system.GetBufferFromEntity<GameModeSnapshotData>();
        m_Char_BanditSnapshotDataFromEntity = system.GetBufferFromEntity<Char_BanditSnapshotData>();
    }
    public bool Deserialize(int serializer, Entity entity, uint snapshot, uint baseline, uint baseline2, uint baseline3,
        ref DataStreamReader reader, NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                return GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeDeserialize(m_Char_CapsuleSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, ref reader, compressionModel);
            case 1:
                return GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeDeserialize(m_PlayerStateSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, ref reader, compressionModel);
            case 2:
                return GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeDeserialize(m_GameModeSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, ref reader, compressionModel);
            case 3:
                return GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeDeserialize(m_Char_BanditSnapshotDataFromEntity, entity, snapshot, baseline, baseline2,
                baseline3, ref reader, compressionModel);
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }
    public void Spawn(int serializer, int ghostId, uint snapshot, ref DataStreamReader reader,
        NetworkCompressionModel compressionModel)
    {
        switch (serializer)
        {
            case 0:
                m_Char_CapsuleSnapshotDataNewGhostIds.Add(ghostId);
                m_Char_CapsuleSnapshotDataNewGhosts.Add(GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeSpawn<Char_CapsuleSnapshotData>(snapshot, ref reader, compressionModel));
                break;
            case 1:
                m_PlayerStateSnapshotDataNewGhostIds.Add(ghostId);
                m_PlayerStateSnapshotDataNewGhosts.Add(GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeSpawn<PlayerStateSnapshotData>(snapshot, ref reader, compressionModel));
                break;
            case 2:
                m_GameModeSnapshotDataNewGhostIds.Add(ghostId);
                m_GameModeSnapshotDataNewGhosts.Add(GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeSpawn<GameModeSnapshotData>(snapshot, ref reader, compressionModel));
                break;
            case 3:
                m_Char_BanditSnapshotDataNewGhostIds.Add(ghostId);
                m_Char_BanditSnapshotDataNewGhosts.Add(GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>.InvokeSpawn<Char_BanditSnapshotData>(snapshot, ref reader, compressionModel));
                break;
            default:
                throw new ArgumentException("Invalid serializer type");
        }
    }

    private BufferFromEntity<Char_CapsuleSnapshotData> m_Char_CapsuleSnapshotDataFromEntity;
    private NativeList<int> m_Char_CapsuleSnapshotDataNewGhostIds;
    private NativeList<Char_CapsuleSnapshotData> m_Char_CapsuleSnapshotDataNewGhosts;
    private BufferFromEntity<PlayerStateSnapshotData> m_PlayerStateSnapshotDataFromEntity;
    private NativeList<int> m_PlayerStateSnapshotDataNewGhostIds;
    private NativeList<PlayerStateSnapshotData> m_PlayerStateSnapshotDataNewGhosts;
    private BufferFromEntity<GameModeSnapshotData> m_GameModeSnapshotDataFromEntity;
    private NativeList<int> m_GameModeSnapshotDataNewGhostIds;
    private NativeList<GameModeSnapshotData> m_GameModeSnapshotDataNewGhosts;
    private BufferFromEntity<Char_BanditSnapshotData> m_Char_BanditSnapshotDataFromEntity;
    private NativeList<int> m_Char_BanditSnapshotDataNewGhostIds;
    private NativeList<Char_BanditSnapshotData> m_Char_BanditSnapshotDataNewGhosts;
}
public struct EnableUnityGroceryGhostReceiveSystemComponent : IComponentData
{}
public class UnityGroceryGhostReceiveSystem : GhostReceiveSystem<UnityGroceryGhostDeserializerCollection>
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireSingletonForUpdate<EnableUnityGroceryGhostReceiveSystemComponent>();
    }
}
