using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine.Profiling;
using Unity.NetCode;
using Unity.Sample.Core;


public class OfflineGameWorld
{
    public OfflineGameWorld(World world, int localPlayerId)
    {
        m_GameWorld = world;

        m_PlayerModule = new PlayerModuleClient(m_GameWorld);

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

        //@TODO: Temp hack for unite keynote to hide error
        Debug.developerConsoleVisible = false;
    }
    void HandleTime(float frameDuration, int tick)
    {
        bool userInputEnabled = InputSystem.GetMousePointerLock();
        m_PlayerModule.SampleInput(m_GameWorld, userInputEnabled, frameDuration, m_RenderTime.tick);
        m_RenderTime.tick = tick;
        m_RenderTime.tickDuration = frameDuration;

        m_PlayerModule.StoreCommand(m_RenderTime.tick);
    }
    public Entity RegisterLocalPlayer(int playerId)
    {
        m_localPlayer = m_PlayerModule.RegisterLocalPlayer(playerId);
        return m_localPlayer;
    }
    public void Update(float frameDuration, int tick)
    {
        HandleTime(frameDuration, tick);
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        gameTimeSystem.SetWorldTime(m_RenderTime);
        gameTimeSystem.frameDuration = frameDuration;
        m_DamageAreaSystem.Update();

        m_PlayerModule.ResolveReferenceFromLocalPlayerToPlayer();
        m_PlayerModule.HandleCommandReset();
        m_PlayerModule.HandleSpawn();

        //m_GameModeSystem.Update();

        m_ManualComponentSystemGroup.Update();

        m_PlayerModule.HandleControlledEntityChanged();

        m_HandleDamageGroup.Update();

        m_ClientLateUpdate.Update();
    }

    bool isDestroyingWorld;
    World m_GameWorld;
    GameTime m_RenderTime = new GameTime(60);

    // External systems

    // Internal systems
    readonly PlayerModuleClient m_PlayerModule;

    readonly DamageAreaSystemServer m_DamageAreaSystem;

    readonly HandleDamageSystemGroup m_HandleDamageGroup;

    readonly ManualComponentSystemGroup m_ManualComponentSystemGroup;

    //readonly GameModeSystemClient m_GameModeSystem;
    readonly ControlledEntityCameraUpdate m_controlledEntityCameraUpdate;
    private readonly ClientLateUpdateGroup m_ClientLateUpdate;

    Entity m_localPlayer;
    uint m_lastCommandTick;
}

[UpdateInGroup(typeof(CustomInitializationSystemGroup))]
[AlwaysUpdateSystem]
public class OfflineGameLoopSystem : ComponentSystem
{
    protected override void OnCreate()
    {
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
        m_StateMachine.Update();
        m_offlineGameWorld?.Update(Time.DeltaTime, UnityEngine.Time.frameCount);
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
