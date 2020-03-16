using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

public struct GameModeSnapshotData : ISnapshotData<GameModeSnapshotData>
{
    public uint tick;
    private int GameModeDatagameTimerSeconds;
    private NativeString64 GameModeDatagameTimerMessage;
    private NativeString64 GameModeDatateamName0;
    private NativeString64 GameModeDatateamName1;
    private int GameModeDatateamScore0;
    private int GameModeDatateamScore1;
    uint changeMask0;

    public uint Tick => tick;
    public int GetGameModeDatagameTimerSeconds(GhostDeserializerState deserializerState)
    {
        return (int)GameModeDatagameTimerSeconds;
    }
    public int GetGameModeDatagameTimerSeconds()
    {
        return (int)GameModeDatagameTimerSeconds;
    }
    public void SetGameModeDatagameTimerSeconds(int val, GhostSerializerState serializerState)
    {
        GameModeDatagameTimerSeconds = (int)val;
    }
    public void SetGameModeDatagameTimerSeconds(int val)
    {
        GameModeDatagameTimerSeconds = (int)val;
    }
    public NativeString64 GetGameModeDatagameTimerMessage(GhostDeserializerState deserializerState)
    {
        return GameModeDatagameTimerMessage;
    }
    public NativeString64 GetGameModeDatagameTimerMessage()
    {
        return GameModeDatagameTimerMessage;
    }
    public void SetGameModeDatagameTimerMessage(NativeString64 val, GhostSerializerState serializerState)
    {
        GameModeDatagameTimerMessage = val;
    }
    public void SetGameModeDatagameTimerMessage(NativeString64 val)
    {
        GameModeDatagameTimerMessage = val;
    }
    public NativeString64 GetGameModeDatateamName0(GhostDeserializerState deserializerState)
    {
        return GameModeDatateamName0;
    }
    public NativeString64 GetGameModeDatateamName0()
    {
        return GameModeDatateamName0;
    }
    public void SetGameModeDatateamName0(NativeString64 val, GhostSerializerState serializerState)
    {
        GameModeDatateamName0 = val;
    }
    public void SetGameModeDatateamName0(NativeString64 val)
    {
        GameModeDatateamName0 = val;
    }
    public NativeString64 GetGameModeDatateamName1(GhostDeserializerState deserializerState)
    {
        return GameModeDatateamName1;
    }
    public NativeString64 GetGameModeDatateamName1()
    {
        return GameModeDatateamName1;
    }
    public void SetGameModeDatateamName1(NativeString64 val, GhostSerializerState serializerState)
    {
        GameModeDatateamName1 = val;
    }
    public void SetGameModeDatateamName1(NativeString64 val)
    {
        GameModeDatateamName1 = val;
    }
    public int GetGameModeDatateamScore0(GhostDeserializerState deserializerState)
    {
        return (int)GameModeDatateamScore0;
    }
    public int GetGameModeDatateamScore0()
    {
        return (int)GameModeDatateamScore0;
    }
    public void SetGameModeDatateamScore0(int val, GhostSerializerState serializerState)
    {
        GameModeDatateamScore0 = (int)val;
    }
    public void SetGameModeDatateamScore0(int val)
    {
        GameModeDatateamScore0 = (int)val;
    }
    public int GetGameModeDatateamScore1(GhostDeserializerState deserializerState)
    {
        return (int)GameModeDatateamScore1;
    }
    public int GetGameModeDatateamScore1()
    {
        return (int)GameModeDatateamScore1;
    }
    public void SetGameModeDatateamScore1(int val, GhostSerializerState serializerState)
    {
        GameModeDatateamScore1 = (int)val;
    }
    public void SetGameModeDatateamScore1(int val)
    {
        GameModeDatateamScore1 = (int)val;
    }

    public void PredictDelta(uint tick, ref GameModeSnapshotData baseline1, ref GameModeSnapshotData baseline2)
    {
        var predictor = new GhostDeltaPredictor(tick, this.tick, baseline1.tick, baseline2.tick);
        GameModeDatagameTimerSeconds = predictor.PredictInt(GameModeDatagameTimerSeconds, baseline1.GameModeDatagameTimerSeconds, baseline2.GameModeDatagameTimerSeconds);
        GameModeDatateamScore0 = predictor.PredictInt(GameModeDatateamScore0, baseline1.GameModeDatateamScore0, baseline2.GameModeDatateamScore0);
        GameModeDatateamScore1 = predictor.PredictInt(GameModeDatateamScore1, baseline1.GameModeDatateamScore1, baseline2.GameModeDatateamScore1);
    }

    public void Serialize(int networkId, ref GameModeSnapshotData baseline, ref DataStreamWriter writer, NetworkCompressionModel compressionModel)
    {
        changeMask0 = (GameModeDatagameTimerSeconds != baseline.GameModeDatagameTimerSeconds) ? 1u : 0;
        changeMask0 |= GameModeDatagameTimerMessage.Equals(baseline.GameModeDatagameTimerMessage) ? 0 : (1u<<1);
        changeMask0 |= GameModeDatateamName0.Equals(baseline.GameModeDatateamName0) ? 0 : (1u<<2);
        changeMask0 |= GameModeDatateamName1.Equals(baseline.GameModeDatateamName1) ? 0 : (1u<<3);
        changeMask0 |= (GameModeDatateamScore0 != baseline.GameModeDatateamScore0) ? (1u<<4) : 0;
        changeMask0 |= (GameModeDatateamScore1 != baseline.GameModeDatateamScore1) ? (1u<<5) : 0;
        writer.WritePackedUIntDelta(changeMask0, baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            writer.WritePackedIntDelta(GameModeDatagameTimerSeconds, baseline.GameModeDatagameTimerSeconds, compressionModel);
        if ((changeMask0 & (1 << 1)) != 0)
            writer.WritePackedStringDelta(GameModeDatagameTimerMessage, baseline.GameModeDatagameTimerMessage, compressionModel);
        if ((changeMask0 & (1 << 2)) != 0)
            writer.WritePackedStringDelta(GameModeDatateamName0, baseline.GameModeDatateamName0, compressionModel);
        if ((changeMask0 & (1 << 3)) != 0)
            writer.WritePackedStringDelta(GameModeDatateamName1, baseline.GameModeDatateamName1, compressionModel);
        if ((changeMask0 & (1 << 4)) != 0)
            writer.WritePackedIntDelta(GameModeDatateamScore0, baseline.GameModeDatateamScore0, compressionModel);
        if ((changeMask0 & (1 << 5)) != 0)
            writer.WritePackedIntDelta(GameModeDatateamScore1, baseline.GameModeDatateamScore1, compressionModel);
    }

    public void Deserialize(uint tick, ref GameModeSnapshotData baseline, ref DataStreamReader reader,
        NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
        changeMask0 = reader.ReadPackedUIntDelta(baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            GameModeDatagameTimerSeconds = reader.ReadPackedIntDelta(baseline.GameModeDatagameTimerSeconds, compressionModel);
        else
            GameModeDatagameTimerSeconds = baseline.GameModeDatagameTimerSeconds;
        if ((changeMask0 & (1 << 1)) != 0)
            GameModeDatagameTimerMessage = reader.ReadPackedStringDelta(baseline.GameModeDatagameTimerMessage, compressionModel);
        else
            GameModeDatagameTimerMessage = baseline.GameModeDatagameTimerMessage;
        if ((changeMask0 & (1 << 2)) != 0)
            GameModeDatateamName0 = reader.ReadPackedStringDelta(baseline.GameModeDatateamName0, compressionModel);
        else
            GameModeDatateamName0 = baseline.GameModeDatateamName0;
        if ((changeMask0 & (1 << 3)) != 0)
            GameModeDatateamName1 = reader.ReadPackedStringDelta(baseline.GameModeDatateamName1, compressionModel);
        else
            GameModeDatateamName1 = baseline.GameModeDatateamName1;
        if ((changeMask0 & (1 << 4)) != 0)
            GameModeDatateamScore0 = reader.ReadPackedIntDelta(baseline.GameModeDatateamScore0, compressionModel);
        else
            GameModeDatateamScore0 = baseline.GameModeDatateamScore0;
        if ((changeMask0 & (1 << 5)) != 0)
            GameModeDatateamScore1 = reader.ReadPackedIntDelta(baseline.GameModeDatateamScore1, compressionModel);
        else
            GameModeDatateamScore1 = baseline.GameModeDatateamScore1;
    }
    public void Interpolate(ref GameModeSnapshotData target, float factor)
    {
    }
}
