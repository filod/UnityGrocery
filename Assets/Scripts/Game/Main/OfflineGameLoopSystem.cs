using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine.Profiling;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Sample.Core;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class OfflineSimulationUpdateSystem : JobComponentSystem
{
    public OfflineGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null)
            GameWorld.Update(Time.DeltaTime, UnityEngine.Time.frameCount);
        return default;
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class OfflineLateUpdateSystem : JobComponentSystem
{
    public OfflineGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null)
            GameWorld.LateUpdate(Time.DeltaTime);
        return default;
    }
}


public class OfflineGameWorld
{
    public OfflineGameWorld(World world, int localPlayerId)
    {
        m_GameWorld = world;

        m_GameModeSystem = m_GameWorld.CreateSystem<GameModeSystemServer>(m_GameWorld);

        m_PlayerModuleClient = new PlayerModuleClient(m_GameWorld);
        m_PlayerModuleServer = new PlayerModuleServer(m_GameWorld);

        m_HandleDamageGroup = m_GameWorld.CreateSystem<HandleDamageSystemGroup>();
        m_DamageAreaSystem = m_GameWorld.CreateSystem<DamageAreaSystemServer>();

        m_ManualComponentSystemGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_ManualComponentSystemGroup.AddSystemToUpdateList(m_GameWorld.CreateSystem<PlayerCharacterControl.PlayerCharacterControlSystem>());

        m_ManualComponentSystemGroup.AddSystemToUpdateList(CharacterModule.CreateClientUpdateSystemGroup(world));
        m_ManualComponentSystemGroup.AddSystemToUpdateList(world.CreateSystem<AbilityUpdateSystemGroup>());

        m_ManualComponentSystemGroup.AddSystemToUpdateList(CharacterModule.CreateClientPresentationSystemGroup(world));
        //m_ManualComponentSystemGroup.AddSystemToUpdateList(CharacterModule.CreateServerPresentationSystemGroup(world));
        m_ManualComponentSystemGroup.AddSystemToUpdateList(CharacterModule.CreateServerUpdateSystemGroup(world));


        //m_GameModeSystem = m_GameWorld.CreateSystem<GameModeSystemClient>();
        m_ClientLateUpdate = m_GameWorld.CreateSystem<ClientLateUpdateGroup>();

        //m_GameModeSystem.SetLocalPlayerId(localPlayerId);

        m_controlledEntityCameraUpdate = m_GameWorld.GetOrCreateSystem<ControlledEntityCameraUpdate>();
        m_controlledEntityCameraUpdate.SortSystemUpdateList();// TODO (mogensh) currently needed because of bug in entities preview.26

    }
    void HandleTime(float frameDuration, int tick)
    {
        bool userInputEnabled = InputSystem.GetMousePointerLock();
        m_RenderTime.tick = tick;
        m_RenderTime.tickDuration = frameDuration;

        if (tick != m_lastCommandTick)
        {
            m_lastCommandTick = (uint)tick;
            m_PlayerModuleClient.ResetInput(userInputEnabled);
        }


        m_PlayerModuleClient.SampleInput(m_GameWorld, userInputEnabled, frameDuration, m_RenderTime.tick);
        m_PlayerModuleClient.StoreCommand(m_RenderTime.tick);
    }
    public Entity RegisterLocalPlayer(int playerId)
    {
        m_localPlayer = m_PlayerModuleClient.RegisterLocalPlayer(playerId);
        return m_localPlayer;
    }
    public void Update(float frameDuration, int tick)
    {
        //GameDebug.Log($"tick {tick}");
        HandleTime(frameDuration, tick);
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        gameTimeSystem.SetWorldTime(m_RenderTime);
        gameTimeSystem.frameDuration = frameDuration;
        m_DamageAreaSystem.Update();

        m_PlayerModuleClient.ResolveReferenceFromLocalPlayerToPlayer();
        m_PlayerModuleClient.HandleCommandReset();
        m_PlayerModuleClient.HandleSpawn();

        m_PlayerModuleClient.RetrieveCommand((uint)tick);
        //m_GameModeSystem.Update();

        m_ManualComponentSystemGroup.Update();

        m_PlayerModuleClient.HandleControlledEntityChanged();

        m_HandleDamageGroup.Update();

        m_ClientLateUpdate.Update();

        m_GameModeSystem.Update();
    }

    public void LateUpdate(float frameDuration)
    {
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        gameTimeSystem.SetWorldTime(m_RenderTime);

        var localPlayerState = m_GameWorld.EntityManager.GetComponentData<LocalPlayer>(m_localPlayer);
        if (localPlayerState.playerEntity != Entity.Null)
        {
            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(localPlayerState.playerEntity);
            if (playerState.controlledEntity != Entity.Null)
            {
                if (m_GameWorld.EntityManager.HasComponent<HealthStateData>(playerState.controlledEntity))
                {
                    var healthState = m_GameWorld.EntityManager.GetComponentData<HealthStateData>(playerState.controlledEntity);
                }

            }
        }

        m_ClientLateUpdate.Update();

        m_controlledEntityCameraUpdate.Update();

        m_PlayerModuleClient.CameraUpdate();

        gameTimeSystem.SetWorldTime(m_RenderTime);

        //gameTimeSystem.SetWorldTime(m_PredictedTime);

    }

    bool isDestroyingWorld;
    World m_GameWorld;
    GameTime m_RenderTime = new GameTime(60);

    // External systems

    // Internal systems
    readonly GameModeSystemServer m_GameModeSystem;
    public readonly PlayerModuleClient m_PlayerModuleClient;
    public readonly PlayerModuleServer m_PlayerModuleServer;

    readonly DamageAreaSystemServer m_DamageAreaSystem;

    readonly HandleDamageSystemGroup m_HandleDamageGroup;

    readonly ManualComponentSystemGroup m_ManualComponentSystemGroup;

    //readonly GameModeSystemClient m_GameModeSystem;
    readonly ControlledEntityCameraUpdate m_controlledEntityCameraUpdate;
    private readonly ClientLateUpdateGroup m_ClientLateUpdate;

    Entity m_localPlayer;
    uint m_lastCommandTick;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[AlwaysUpdateSystem]
public class OfflineGameLoopSystem : ComponentSystem
{
    protected override void OnCreate()
    {
        if (!GameBootStrap.offline) return;
        GameDebug.Log("OnCreate: OfflineGameLoopSystem");
        m_GameWorld = World;

        World.GetOrCreateSystem<GhostPredictionSystemGroup>();
        World.GetOrCreateSystem<GameTimeSystem>();
        m_StateMachine = new StateMachine<OfflineState>();

        m_StateMachine.Add(OfflineState.WaitSubscene, null, UpdateWaitSubsceneState, null);
        m_StateMachine.Add(OfflineState.Playing, EnterPlayingState, UpdatePlayingState, null);
        m_StateMachine.SwitchTo(m_GameWorld, OfflineState.WaitSubscene);
    }
    protected override void OnUpdate()
    {
        if (!GameBootStrap.offline) return;
        m_StateMachine.Update();
        //m_offlineGameWorld?.Update(Time.DeltaTime, UnityEngine.Time.frameCount);
    }
    protected override void OnDestroy()
    {
        PrefabAssetManager.Shutdown();
    }
    void UpdateWaitSubsceneState()
    {
        // TODO (mogensh) we should find a better way to make sure subscene is loaded (this uses knowledge of what is in subscene)
        var query = m_GameWorld.EntityManager.CreateEntityQuery(typeof(HeroRegistry.RegistryEntity));
        var ready = query.CalculateEntityCount() > 0;
        query.Dispose();
        if (ready)
            m_StateMachine.SwitchTo(m_GameWorld, OfflineState.Playing);
    }
    void EnterPlayingState() {
        var clientId = 2333;
        m_offlineGameWorld = new OfflineGameWorld(m_GameWorld, clientId);
        m_LocalPlayer = m_offlineGameWorld.RegisterLocalPlayer(clientId);
        var entityManager = m_GameWorld.EntityManager;
        var playerEntity = m_offlineGameWorld.m_PlayerModuleServer.CreatePlayerEntity(m_GameWorld, clientId, 0, "", true);
        entityManager.AddBuffer<UserCommand>(playerEntity);
        Console.SetOpen(false);
        //entityManager.SetComponentData(client, new CommandTargetComponent { targetEntity = playerEntity });
        m_GameWorld.GetExistingSystem<OfflineLateUpdateSystem>().GameWorld = m_offlineGameWorld;
        m_GameWorld.GetExistingSystem<OfflineSimulationUpdateSystem>().GameWorld = m_offlineGameWorld;
    }
    void UpdatePlayingState()
    {
    }
    enum OfflineState
    {
        WaitSubscene,
        Playing,
    }
    StateMachine<OfflineState> m_StateMachine;
    OfflineGameWorld m_offlineGameWorld;
    World m_GameWorld;
    Entity m_LocalPlayer;
}
