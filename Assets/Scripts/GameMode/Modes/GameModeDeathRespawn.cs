using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Sample.Core;

// Simple, team based deathmatch mode

public class GameModeDeathRespawn : IGameMode
{
    [ConfigVar(Name = "game.dm.roundlength", DefaultValue = "18000", Description = "Deathmatch round length (seconds)")]
    public static ConfigVar roundLength;
    public void Initialize(World world, GameModeSystemServer gameModeSystemServer)
    {
        m_world = world;
        m_GameModeSystemServer = gameModeSystemServer;

        Console.Write("DeathRespawn game mode initialized");
    }

    public void Restart()
    {
        m_GameModeSystemServer.StartGameTimer(roundLength, "GameTimeLength");
    }

    public void Shutdown()
    {
    }

    public void Update()
    {
    }

    public void OnPlayerJoin(ref Player.State playerState)
    {
        GameDebug.Log($"Player join: {playerState.playerId}");
    }

    public void OnPlayerKilled(ref Player.State victim, ref Player.State killer)
    {
    }

    public void OnPlayerRespawn(ref Player.State playerState, ref Vector3 position, ref Quaternion rotation)
    {
        m_GameModeSystemServer.GetRandomSpawnTransform(ref position, ref rotation);
    }

    World m_world;
    GameModeSystemServer m_GameModeSystemServer;
}
