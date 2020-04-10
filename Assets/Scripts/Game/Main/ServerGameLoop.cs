using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using Unity.Animation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Networking.Transport;
using Unity.Scenes;
using Unity.NetCode;
using Unity.Sample.Core;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateBefore(typeof(GhostSimulationSystemGroup))]
[AlwaysUpdateSystem]
[AlwaysSynchronizeSystem]
public class BeforeServerPredictionSystem : JobComponentSystem
{
    public ServerGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var gameWorld = GameWorld;
        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithNone<NetworkStreamConnection>()
            .WithAll<AcceptedConnectionStateComponent>()
            .WithNativeDisableContainerSafetyRestriction(PostUpdateCommands)
            .WithoutBurst() // Captures managed data
            .ForEach((Entity entity) =>
            {
                if (gameWorld != null)
                    gameWorld.HandleClientDisconnect(PostUpdateCommands,entity);
                PostUpdateCommands.RemoveComponent<AcceptedConnectionStateComponent>(entity);
            }).Run();
        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();

        if (GameWorld != null)
            GameWorld.BeforePredictionUpdate();
        return default;
    }
}
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
[UpdateAfter(typeof(GhostSimulationSystemGroup))]
[UpdateBefore(typeof(AnimationSystemGroup))]
[AlwaysSynchronizeSystem]
public class AfterServerPredictionSystem : JobComponentSystem
{
    public ServerGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null)
            GameWorld.AfterPredictionUpdate();
        return default;
    }
}
[UpdateInGroup(typeof(GhostPredictionSystemGroup))]
[AlwaysSynchronizeSystem]
public class ServerPredictionSystem : JobComponentSystem
{
    public ServerGameWorld GameWorld;
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        if (GameWorld != null)
            GameWorld.PredictionUpdate();
        return default;
    }
}

struct AcceptedConnectionStateComponent : ISystemStateComponentData
{
    public Entity playerEntity;
    public bool isReady;
}

public class ServerGameWorld
{
    public ServerGameWorld(World world)
    {
        m_GameWorld = world;

        m_PlayerModule = new PlayerModuleServer(m_GameWorld);

        m_GameModeSystem = m_GameWorld.CreateSystem<GameModeSystemServer>(m_GameWorld);

        m_HandleDamageGroup = m_GameWorld.CreateSystem<HandleDamageSystemGroup>();

        //m_TeleporterSystem = m_GameWorld.CreateSystem<TeleporterSystemServer>();

        m_DamageAreaSystem = m_GameWorld.CreateSystem<DamageAreaSystemServer>();

        m_HandleControlledEntityChangedGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_HandleControlledEntityChangedGroup.AddSystemToUpdateList(m_GameWorld.CreateSystem<PlayerCharacterControl.PlayerCharacterControlSystem>());

        m_PredictedUpdateGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_PredictedUpdateGroup.AddSystemToUpdateList(CharacterModule.CreateServerUpdateSystemGroup(world));
        m_PredictedUpdateGroup.AddSystemToUpdateList(world.CreateSystem<AbilityUpdateSystemGroup>());

        m_AfterPredictionUpdateGroup = m_GameWorld.CreateSystem<ManualComponentSystemGroup>();
        m_AfterPredictionUpdateGroup.AddSystemToUpdateList(CharacterModule.CreateServerPresentationSystemGroup(world));
        m_AfterPredictionUpdateGroup.AddSystemToUpdateList(m_GameWorld.GetOrCreateSystem(typeof(PartSystemUpdateGroup)));

    }

    public void Shutdown(bool isDestroyingWorld)
    {
        m_PlayerModule.Shutdown();

        // When destroying the world all systems will be torn down - so no need to do it manually
        if (!isDestroyingWorld)
        {
            m_HandleDamageGroup.DestroyGroup();

            m_GameWorld.DestroySystem(m_HandleControlledEntityChangedGroup);
            m_GameWorld.DestroySystem(m_PredictedUpdateGroup);
            m_GameWorld.DestroySystem(m_AfterPredictionUpdateGroup);
            m_GameWorld.DestroySystem(m_DamageAreaSystem);
        }


        AnimationGraphHelper.Shutdown(m_GameWorld);

        m_GameWorld = null;
    }

    public void RespawnPlayer(Entity playerEntity)
    {
        var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
        if (playerState.controlledEntity == Entity.Null)
            return;

        if (m_GameWorld.EntityManager.HasComponent<Character.State>(playerState.controlledEntity))
            CharacterDespawnRequest.Create(m_GameWorld, playerState.controlledEntity);

        playerState.controlledEntity = Entity.Null;

        m_GameWorld.EntityManager.SetComponentData(playerEntity, playerState);
    }


    public void HandleClientCommands()
    {
        var connectionQuery = m_GameWorld.EntityManager.CreateEntityQuery(
            ComponentType.ReadWrite<NetworkIdComponent>(),
            ComponentType.ReadWrite<CommandTargetComponent>());
        var commandTargets = connectionQuery.ToComponentDataArray<CommandTargetComponent>(Allocator.TempJob);
        for (int i = 0; i < commandTargets.Length; ++i)
        {
            var targetEntity = commandTargets[i].targetEntity;
            if (targetEntity == Entity.Null)
                continue;
            m_GameWorld.EntityManager.GetBuffer<UserCommand>(targetEntity)
                .GetDataAtTick(m_GameWorld.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick, out var latestCommand);

            // Pass on command to controlled entity
            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(targetEntity);
            if (playerState.controlledEntity != Entity.Null)
            {
                var userCommand = m_GameWorld.EntityManager.GetComponentData<PlayerControlled.State>(
                    playerState.controlledEntity);

                userCommand.prevCommand = userCommand.command;
                userCommand.command = latestCommand;

                m_GameWorld.EntityManager.SetComponentData(playerState.controlledEntity, userCommand);
            }
        }
        commandTargets.Dispose();
    }

    public void BeforePredictionUpdate()
    {
        var gameTimeSystem = m_GameWorld.GetExistingSystem<GameTimeSystem>();
        var time = gameTimeSystem.GetWorldTime();
        time.tick = (int)m_GameWorld.GetExistingSystem<ServerSimulationSystemGroup>().ServerTick;
        time.tickDuration = time.tickInterval;
        gameTimeSystem.SetWorldTime(time);
        gameTimeSystem.frameDuration = time.tickInterval;

        Profiler.BeginSample("HandleClientCommands");

        // This call backs into ProcessCommand
        HandleClientCommands();

        Profiler.EndSample();

        GameTime gameTime = new GameTime(gameTimeSystem.GetWorldTime().tickRate);
        gameTime.SetTime(gameTimeSystem.GetWorldTime().tick, gameTimeSystem.GetWorldTime().tickInterval);

        // Handle controlled entity changed
        m_HandleControlledEntityChangedGroup.Update();

    }

    public void PredictionUpdate()
    {
        m_PredictedUpdateGroup.Update();
    }

    public void AfterPredictionUpdate()
    {
        m_DamageAreaSystem.Update();

        // Handle damage
        m_HandleDamageGroup.Update();

        // TODO (mogensh) for now we upadte this AFTER CharacterModule as we depend on AnimSourceCtrl to run before bodypart. Sort this out
        m_AfterPredictionUpdateGroup.Update();

        // Update gamemode. Run last to allow picking up deaths etc.
        m_GameModeSystem.Update();
    }

    public void HandleClientConnect(Entity client)
    {
        var entityManager = m_GameWorld.EntityManager;
        bool isReady = entityManager.GetComponentData<AcceptedConnectionStateComponent>(client).isReady;
        var playerEntity = m_PlayerModule.CreatePlayerEntity(m_GameWorld, entityManager.GetComponentData<NetworkIdComponent>(client).Value, 0, "", isReady);
        entityManager.AddBuffer<UserCommand>(playerEntity);
        entityManager.SetComponentData(client, new CommandTargetComponent{targetEntity = playerEntity});
        entityManager.SetComponentData(client, new AcceptedConnectionStateComponent {playerEntity = playerEntity, isReady = isReady});
    }

    public void HandleClientDisconnect(EntityCommandBuffer ecb, Entity client)
    {
        var entityManager = m_GameWorld.EntityManager;
        var playerEntity = entityManager.GetComponentData<AcceptedConnectionStateComponent>(client).playerEntity;
        if (playerEntity == Entity.Null)
            return;

        CharacterModule.ServerCleanupPlayer(m_GameWorld, ecb, playerEntity);
        m_PlayerModule.CleanupPlayer(playerEntity);
    }

    // Internal systems
    World m_GameWorld;
    readonly PlayerModuleServer m_PlayerModule;

    //readonly ServerCameraSystem m_CameraSystem;
    readonly GameModeSystemServer m_GameModeSystem;

    readonly ManualComponentSystemGroup m_HandleControlledEntityChangedGroup;
    readonly ManualComponentSystemGroup m_PredictedUpdateGroup;
    readonly ManualComponentSystemGroup m_AfterPredictionUpdateGroup;

    readonly DamageAreaSystemServer m_DamageAreaSystem;
    //readonly TeleporterSystemServer m_TeleporterSystem;

    readonly HandleDamageSystemGroup m_HandleDamageGroup;

    //readonly MovableSystemServer m_MoveableSystem;
}

[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
[AlwaysSynchronizeSystem]
//[DisableAutoCreation]
public class ServerGameLoopSystem : JobComponentSystem
{
    [ConfigVar(Name = "server.printstatus", DefaultValue = "0", Description = "Print status line every <n> ticks")]
    public static ConfigVar serverPrintStatus;

    public void Shutdown()
    {
        m_StateMachine.SwitchTo(null, ServerState.Idle);
    }
    protected override void OnCreate()
    {
        GameDebug.Log("OnCreate: ServerGameLoopSystem");
        var args = new string[0];
        World.GetOrCreateSystem<SceneSystem>().BuildConfigurationGUID = new Unity.Entities.Hash128("5e6f048560f5a14489260690809fec6e");

        var tickRate = EntityManager.CreateEntity();
        EntityManager.AddComponentData(tickRate, new ClientServerTickRate
        {
            MaxSimulationStepsPerFrame = 4,
            // Hardcoded for now, should be a setting.
            NetworkTickRate = Game.serverTickRate.IntValue / 3,
            SimulationTickRate = Game.serverTickRate.IntValue,
            TargetFrameRateMode = Game.IsHeadless() ? ClientServerTickRate.FrameRateMode.Sleep : ClientServerTickRate.FrameRateMode.BusyWait
        });

        m_ClientsQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<AcceptedConnectionStateComponent>(), ComponentType.ReadWrite<NetworkStreamConnection>());

        // Set up statemachine for ServerGame
        m_StateMachine = new StateMachine<ServerState>();
        m_StateMachine.Add(ServerState.Idle, null, UpdateIdleState, null);
        m_StateMachine.Add(ServerState.Loading, null, UpdateLoadingState, null);
        m_StateMachine.Add(ServerState.WaitSubscene, null, UpdateWaitSubscene, null);
        m_StateMachine.Add(ServerState.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);

        m_StateMachine.SwitchTo(null,ServerState.Idle);

        var ep = NetworkEndPoint.AnyIpv4;
        ep.Port = (ushort) (NetworkConfig.serverPort.IntValue);
        World.GetOrCreateSystem<NetworkStreamReceiveSystem>().Listen(ep);

        var listenAddresses = NetworkUtils.GetLocalInterfaceAddresses();
        if (listenAddresses.Count > 0)
            Console.SetPrompt(listenAddresses[0] + ":" + NetworkConfig.serverPort.Value + "> ");
        GameDebug.Log("Listening on " + string.Join(", ", NetworkUtils.GetLocalInterfaceAddresses()) + " on port " + NetworkConfig.serverPort.IntValue);

        //m_ServerQueryProtocolServer = new SQP.SQPServer(NetworkConfig.serverSQPPort.IntValue > 0 ? NetworkConfig.serverSQPPort.IntValue : NetworkConfig.serverPort.IntValue + NetworkConfig.sqpPortOffset);


        m_GameWorld = World;
        World.CreateSystem<GameTimeSystem>();

        GameDebug.Log("Network server initialized");

        Console.AddCommand("s_respawn", CmdRespawn, "Respawn character (usage : respawn playername|playerId)", this.GetHashCode());

#if UNITY_EDITOR
        if (GameBootStrap.IsSingleLevelPlaymode)
            m_StateMachine.SwitchTo(World, ServerState.Loading);
        else
#endif
        InputSystem.SetMousePointerLock(false);

        m_ServerStartTime = (float)Time.ElapsedTime; 

        GameDebug.Log("Server initialized");
        Console.SetOpen(false);
    }

    protected override void OnDestroy()
    {
        m_isDestroyingWorld = true;
        GameDebug.Log("ServerGameState shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();

        PrefabAssetManager.Shutdown();
        m_GameWorld = null;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int clientCount = m_ClientsQuery.CalculateEntityCount();
        if (clientCount > m_MaxClients)
            m_MaxClients = clientCount;


        m_StateMachine.Update();

        return default;
    }

    public ServerGameWorld GetServerGameWorld()
    {
        return m_serverGameWorld;
    }

    public void OnConnect(Entity client)
    {
        // TODO (timj) disconnect if server is full
        m_GameWorld.EntityManager.AddComponent<AcceptedConnectionStateComponent>(client);

        if (m_serverGameWorld != null)
            m_serverGameWorld.HandleClientConnect(client);
    }

    /// <summary>
    /// Idle state, no level is loaded
    /// </summary>
    void UpdateIdleState()
    {
    }

    /// <summary>
    /// Loading state, load in progress
    /// </summary>
    void UpdateLoadingState()
    {
        if (GameBootStrap.IsSingleLevelPlaymode /*Game.game.levelManager.IsCurrentLevelLoaded()*/)
            m_StateMachine.SwitchTo(m_GameWorld,ServerState.WaitSubscene);
    }

    void UpdateWaitSubscene()
    {
        // TODO (mogensh) we should find a better way to make sure subscene is loaded (this uses knowledge of what is in subscene)
        var query = m_GameWorld.EntityManager.CreateEntityQuery(typeof(HeroRegistry.RegistryEntity));
        var ready = query.CalculateEntityCount() > 0;
        query.Dispose();
        if(ready)
            m_StateMachine.SwitchTo(m_GameWorld,ServerState.Active);
    }



    /// <summary>
    /// Active state, level loaded
    /// </summary>
    void EnterActiveState()
    {
        GameDebug.Assert(m_serverGameWorld == null);

        m_serverGameWorld = new ServerGameWorld(m_GameWorld);
        var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < clients.Length; ++i)
        {
            m_serverGameWorld.HandleClientConnect(clients[i]);
        }
        clients.Dispose();

        var entity = m_GameWorld.EntityManager.CreateEntity();// Game state entity
        m_GameWorld.EntityManager.AddComponentData(entity, new ActiveStateComponentData { MapId = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(0).buildIndex });
        m_GameWorld.GetExistingSystem<BeforeServerPredictionSystem>().GameWorld = m_serverGameWorld;
        m_GameWorld.GetExistingSystem<ServerPredictionSystem>().GameWorld = m_serverGameWorld;
        m_GameWorld.GetExistingSystem<AfterServerPredictionSystem>().GameWorld = m_serverGameWorld;
    }

    void UpdateActiveState()
    {
    }

    void LeaveActiveState()
    {
        if (Unity.Entities.World.All.Contains(World))
        {
            m_GameWorld.GetExistingSystem<BeforeServerPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<ServerPredictionSystem>().GameWorld = null;
            m_GameWorld.GetExistingSystem<AfterServerPredictionSystem>().GameWorld = null;
        }

        m_serverGameWorld.Shutdown(m_isDestroyingWorld);
        m_serverGameWorld = null;
    }

    void CmdRespawn(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Write("Invalid argument for respawn command (usage : respawn playername|playerId)");
            return;
        }

        var playerId = -1;
        var playerName = args[0];
        var usePlayerId = int.TryParse(args[0], out playerId);

        var entityManager = m_GameWorld.EntityManager;
        var clients = m_ClientsQuery.ToEntityArray(Allocator.TempJob);
        bool found = false;
        for (int i = 0; i < clients.Length; ++i)
        {
            var playerEntity = entityManager.GetComponentData<AcceptedConnectionStateComponent>(clients[i]).playerEntity;
            if (playerEntity == Entity.Null)
                continue;

            var clientId = entityManager.GetComponentData<NetworkIdComponent>(clients[i]).Value;
            if (usePlayerId && clientId != playerId)
                continue;

            var playerState = m_GameWorld.EntityManager.GetComponentData<Player.State>(playerEntity);
            //if (!usePlayerId && playerState.playerName.ToString() != playerName)
            //    continue; 

            m_serverGameWorld.RespawnPlayer(playerEntity);
            found = true;
            break;
        }
        clients.Dispose();

        if(!found)
            GameDebug.Log("Could not find character. Unknown player, invalid character id or player doesn't have a character: " + args[0]);
    }

    // Statemachine
    enum ServerState
    {
        Idle,
        Loading,
        WaitSubscene,
        Active,
    }
    StateMachine<ServerState> m_StateMachine;

    World m_GameWorld;

    ServerGameWorld m_serverGameWorld;
    public double m_nextTickTime = 0;

    float m_ServerStartTime;
    int m_MaxClients;
    EntityQuery m_ClientsQuery;
    private bool m_isDestroyingWorld;
}
