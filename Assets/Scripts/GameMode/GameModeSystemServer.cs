using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Sample.Core;
using Unity.Transforms;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public interface IGameMode
{
    void Initialize(World world, GameModeSystemServer gameModeSystemServer);
    void Shutdown();

    void Restart();
    void Update();

    void OnPlayerJoin(ref Player.State playerState);
    void OnPlayerRespawn(ref Player.State player, ref Vector3 position, ref Quaternion rotation);
    void OnPlayerKilled(ref Player.State victim, ref Player.State killer);
}

public class NullGameMode : IGameMode
{
    public void Initialize(World world, GameModeSystemServer gameModeSystemServer) {}
    public void OnPlayerJoin(ref Player.State playerState) {}
    public void OnPlayerKilled(ref Player.State victim, ref Player.State killer) {}
    public void OnPlayerRespawn(ref Player.State player, ref Vector3 position, ref Quaternion rotation) {}
    public void Restart() {}
    public void Shutdown() {}
    public void Update() {}
}

public class Team
{
    public string name;
    public int score;
}

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class GameModeSystemServer : JobComponentSystem
{
    [ConfigVar(Name = "game.respawndelay", DefaultValue = "2", Description = "Time from death to respawning")]
    public static ConfigVar respawnDelay;
    [ConfigVar(Name = "game.modename", DefaultValue = "deathmatch", Description = "Which gamemode to use")]
    public static ConfigVar modeName;

    public EntityQuery playersComponentGroup;
    //EntityQuery m_TeamBaseComponentGroup;
    EntityQuery m_SpawnPointComponentGroup;
    EntityQuery m_PlayersComponentGroup;

    public Entity gameModeEntity;
    //public readonly ChatSystemServer chatSystem;
    //public List<Team> teams = new List<Team>();
    //public List<TeamBase> teamBases = new List<TeamBase>();

    public GameModeSystemServer(World world/*, ChatSystemServer chatSystem*/)
    {
        m_World = world;
        //this.chatSystem = chatSystem;
        m_CurrentGameModeName = "";
    }

    public void Restart()
    {
        GameDebug.Log("Restarting gamemode");

        m_EnableRespawning = true;

        m_GameMode.Restart();

        //chatSystem.ResetChatTime();
    }

    public void Shutdown()
    {
        m_GameMode.Shutdown();

        PrefabAssetManager.DestroyEntity(m_World.EntityManager, gameModeEntity);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        playersComponentGroup = GetEntityQuery(typeof(Player.State));
        m_SpawnPointComponentGroup = GetEntityQuery(typeof(SpawnPoint.State));
        m_PlayersComponentGroup = GetEntityQuery(typeof(Player.State), typeof(PlayerCharacterControl.State));

        // Create game mode state

        //gameModeEntity = PrefabAssetManager.CreateEntity(World, RepEntityType.GameMode);
        var reg = EntityManager.GetComponentData<GlobalAssetRegistry>(GetEntityQuery(typeof(GlobalAssetRegistry)).GetSingletonEntity());
        gameModeEntity = PrefabAssetManager.CreateEntity(World.EntityManager, reg.gameModePrefab);
    }

    new public EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
    {
        return base.GetEntityQuery(componentTypes);
    }

    float m_TimerStart;
    ConfigVar m_TimerLength;
    public void StartGameTimer(ConfigVar seconds, string message)
    {
        m_TimerStart = (float)Time.ElapsedTime;
        m_TimerLength = seconds;

        var gameModeState = EntityManager.GetComponentData<GameModeData>(gameModeEntity);
        gameModeState.gameTimerMessage.CopyFrom(message);
        EntityManager.SetComponentData(gameModeEntity, gameModeState);
    }

    public int GetGameTimer()
    {
        return Mathf.Max(0, Mathf.FloorToInt(m_TimerStart + m_TimerLength.FloatValue - (float)Time.ElapsedTime));
    }

    public void SetRespawnEnabled(bool enable)
    {
        m_EnableRespawning = enable;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Handle change of game mode
        if (m_CurrentGameModeName != modeName.Value)
        {
            m_CurrentGameModeName = modeName.Value;

            switch (m_CurrentGameModeName)
            {
                case "deathmatch":
                    m_GameMode = new GameModeDeathRespawn();
                    break;
                default:
                    m_GameMode = new NullGameMode();
                    break;
            }
            m_GameMode.Initialize(m_World, this);
            GameDebug.Log("New gamemode : '" + m_GameMode.GetType().ToString() + "'");
            Restart();
            return default;
        }

        var playerStateArray = m_PlayersComponentGroup.ToComponentDataArray<Player.State>(Allocator.TempJob);
        var playerEntityArray = m_PlayersComponentGroup.ToEntityArray(Allocator.TempJob);
        var playerCharCtrlArray = m_PlayersComponentGroup.ToComponentDataArray<PlayerCharacterControl.State>(Allocator.TempJob);


        //Handle joining players
        for (int i = 0, c = playerStateArray.Length; i < c; ++i)
        {
            var playerEntity = playerEntityArray[i];
            var playerState = playerStateArray[i];
            if (!playerState.gameModeSystemInitialized)
            {
                m_GameMode.OnPlayerJoin(ref playerState);
                playerState.gameModeSystemInitialized = true;

                EntityManager.SetComponentData(playerEntity, playerState);
            }
        }

        //playerStateArray.Dispose();


        m_GameMode.Update();

        // General rules
        var gameModeState = EntityManager.GetComponentData<GameModeData>(gameModeEntity);
        gameModeState.gameTimerSeconds = GetGameTimer();
        EntityManager.SetComponentData(gameModeEntity, gameModeState);

        //playerStateArray = m_PlayersComponentGroup.ToComponentDataArray<Player.State>(Allocator.TempJob);

        var PostUpdateCommands = new EntityCommandBuffer(Allocator.TempJob);
        for (int i = 0, c = playerStateArray.Length; i < c; ++i)
        {
            var playerState = playerStateArray[i];
            var controlledEntity = playerState.controlledEntity;
            var playerEntity = playerEntityArray[i];


            //playerState.actionString = new NativeString64(playerState.enableCharacterSwitch ? "Press H to change character" : "");

            var charControl = playerCharCtrlArray[i];

            // Spawn contolled entity (character) any missing
            if (controlledEntity == Entity.Null)
            {
                var position = new Vector3(0.0f, 0.2f, 0.0f);
                var rotation = Quaternion.identity;
                GetRandomSpawnTransform(ref position, ref rotation);

                m_GameMode.OnPlayerRespawn(ref playerState, ref position, ref rotation);
                EntityManager.SetComponentData(playerEntity,playerState);

                if (charControl.characterType == -1)
                {
                    charControl.characterType = Game.characterType.IntValue;
                }

                EntityManager.SetComponentData(playerEntity,charControl);

                CharacterSpawnRequest.Create(PostUpdateCommands, charControl.characterType, position, rotation, playerEntity);

                continue;
            }

            // Has new new entity been requested
            if (charControl.requestedCharacterType != -1)
            {
                if (charControl.requestedCharacterType != charControl.characterType)
                {
                    charControl.characterType = charControl.requestedCharacterType;
                    if (playerState.controlledEntity != Entity.Null)
                    {
                        // Despawn current controlled entity. New entity will be created later
                        if (EntityManager.HasComponent<Character.State>(controlledEntity))
                        {
                            var predictedState = EntityManager.GetComponentData<Character.PredictedData>(controlledEntity);
                            var rotation = math.length(predictedState.velocity) > 0.01f ? Quaternion.LookRotation(math.normalize(predictedState.velocity)) : Quaternion.identity;

                            CharacterDespawnRequest.Create(PostUpdateCommands, controlledEntity);
                            CharacterSpawnRequest.Create(PostUpdateCommands, charControl.characterType, predictedState.position, rotation, playerEntity);
                        }
                        playerState.controlledEntity = Entity.Null;
                        EntityManager.SetComponentData(playerEntity, playerState);
                    }
                }
                charControl.requestedCharacterType = -1;

                EntityManager.SetComponentData(playerEntity,charControl);

                continue;
            }

            if (EntityManager.HasComponent<HealthStateData>(controlledEntity))
            {
                // Is character dead ?
                var healthState = EntityManager.GetComponentData<HealthStateData>(controlledEntity);
                if (healthState.health == 0)
                {
                    var gameTimeSystem = m_World.GetExistingSystem<GameTimeSystem>();

                    // Send kill msg
                    if (healthState.deathTick == gameTimeSystem.GetWorldTime().tick)
                    {
                        var killerEntity = healthState.killedBy;
                        var killerIndex = FindPlayerControlling(playerStateArray, killerEntity);
                        if (killerIndex != -1)
                        {
                            var killerState = playerStateArray[killerIndex];
                            var format = s_KillMessages[Random.Range(0, s_KillMessages.Length)];
                            m_GameMode.OnPlayerKilled(ref playerState, ref killerState);

                            EntityManager.SetComponentData(playerEntity, playerState);
                            EntityManager.SetComponentData(playerEntityArray[killerIndex], killerState);
                        }
                        else
                        {
                            var format = s_SuicideMessages[Random.Range(0, s_SuicideMessages.Length)];

                            m_GameMode.OnPlayerKilled(ref playerState, ref playerState);
                            EntityManager.SetComponentData(playerEntity, playerState);
                        }
                    }

                    // Respawn dead players except if in ended mode
                    if (m_EnableRespawning && (gameTimeSystem.GetWorldTime().tick - healthState.deathTick) *
                        gameTimeSystem.GetWorldTime().tickInterval > respawnDelay.IntValue)
                    {
                        // Despawn current controlled entity. New entity will be created later
                        if (EntityManager.HasComponent<Character.State>(controlledEntity))
                            CharacterDespawnRequest.Create(PostUpdateCommands, controlledEntity);
                        playerState.controlledEntity = Entity.Null;
                        EntityManager.SetComponentData(playerEntity, playerState);
                    }
                }
            }
        }

        PostUpdateCommands.Playback(EntityManager);
        PostUpdateCommands.Dispose();

        playerStateArray.Dispose();
        playerEntityArray.Dispose();
        playerCharCtrlArray.Dispose();
        return default;
    }

    int FindPlayerControlling(NativeArray<Player.State> players, Entity entity)
    {
        if (entity == Entity.Null)
            return -1;

        for (int i = 0, c = players.Length; i < c; ++i)
        {
            var playerState = players[i];
            if (playerState.controlledEntity == entity)
                return i;
        }
        return -1;
    }

    public bool GetRandomSpawnTransform(ref Vector3 pos, ref Quaternion rot)
    {
        // Make list of spawnpoints for team
        //using (var spawnPointArray = m_SpawnPointComponentGroup.ToComponentDataArray<SpawnPoint.State>(Allocator.TempJob))
        using (var spawnPointEntityArray = m_SpawnPointComponentGroup.ToEntityArray(Allocator.TempJob))
        {
            var rndIndex = Random.Range(0, spawnPointEntityArray.Length);

            pos = m_World.EntityManager.GetComponentData<Translation>(spawnPointEntityArray[rndIndex]).Value;
            rot = m_World.EntityManager.GetComponentData<Rotation>(spawnPointEntityArray[rndIndex]).Value;

#if UNITY_EDITOR
            GameDebug.Log("spawning at " + m_World.EntityManager.GetName(spawnPointEntityArray[rndIndex]));
#endif
        }

        return true;
    }

    static string[] s_KillMessages = new string[]
    {
        "<color={2}>{0}</color> killed <color={3}>{1}</color>",
        "<color={2}>{0}</color> terminated <color={3}>{1}</color>",
        "<color={2}>{0}</color> ended <color={3}>{1}</color>",
        "<color={2}>{0}</color> owned <color={3}>{1}</color>",
    };

    static string[] s_SuicideMessages = new string[]
    {
        "<color={1}>{0}</color> rebooted",
        "<color={1}>{0}</color> gave up",
        "<color={1}>{0}</color> slipped and accidently killed himself",
        "<color={1}>{0}</color> wanted to give the enemy team an edge",
    };

    static string[] m_TeamColors = new string[]
    {
        "#1EA00000", //"#FF19E3FF",
        "#1EA00001", //"#00FFEAFF",
    };

    readonly World m_World;
    int[] m_prevTeamSpawnPointIndex = new int[2];
    IGameMode m_GameMode;
    bool m_EnableRespawning = true;
    string m_CurrentGameModeName;
}
