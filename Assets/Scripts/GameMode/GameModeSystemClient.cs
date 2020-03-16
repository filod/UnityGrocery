using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Sample.Core;
using UnityEngine.UI;

[DisableAutoCreation]
[AlwaysSynchronizeSystem]
public class GameModeSystemClient : JobComponentSystem
{
    EntityQuery PlayersGroup;
    EntityQuery GameModesGroup;

    int m_PlayerId;
    Entity m_Player;

    public GameModeSystemClient()
    {
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        PlayersGroup = GetEntityQuery(typeof(Player.State));
        GameModesGroup = GetEntityQuery(typeof(GameModeData));
    }

    public void Shutdown()
    {
    }

    // TODO : We need to fix up these dependencies
    public void SetLocalPlayerId(int playerId)
    {
        m_PlayerId = playerId;
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {

        using (var playerEntityArray = PlayersGroup.ToEntityArray(Allocator.Persistent))
        using (var playerStateArray = PlayersGroup.ToComponentDataArray<Player.State>(Allocator.Persistent))
        using (var gameModeArray = GameModesGroup.ToComponentDataArray<GameModeData>(Allocator.Persistent))
        {
            // Update individual player stats
            // Use these indexes to fill up each of the team lists

            for (int i = 0, c = playerStateArray.Length; i < c; ++i)
            {
                var playerEntity = playerEntityArray[i];
                var playerState = playerStateArray[i];

                // TODO (petera) this feels kind of hacky
                if (playerState.playerId == m_PlayerId)
                    m_Player = playerEntity;
            }


            if (m_Player == Entity.Null)
                return default;

            var localPlayerState = EntityManager.GetComponentData<Player.State>(m_Player);

            // Update gamemode overlay
            GameDebug.Assert(gameModeArray.Length < 2);
        }

        return default;
    }
}
