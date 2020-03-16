using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;

public struct PlayerStateSnapshotData : ISnapshotData<PlayerStateSnapshotData>
{
    public uint tick;
    private int PlayerStateplayerId;
    private uint PlayerStategameModeSystemInitialized;
    uint changeMask0;

    public uint Tick => tick;
    public int GetPlayerStateplayerId(GhostDeserializerState deserializerState)
    {
        return (int)PlayerStateplayerId;
    }
    public int GetPlayerStateplayerId()
    {
        return (int)PlayerStateplayerId;
    }
    public void SetPlayerStateplayerId(int val, GhostSerializerState serializerState)
    {
        PlayerStateplayerId = (int)val;
    }
    public void SetPlayerStateplayerId(int val)
    {
        PlayerStateplayerId = (int)val;
    }
    public bool GetPlayerStategameModeSystemInitialized(GhostDeserializerState deserializerState)
    {
        return PlayerStategameModeSystemInitialized!=0;
    }
    public bool GetPlayerStategameModeSystemInitialized()
    {
        return PlayerStategameModeSystemInitialized!=0;
    }
    public void SetPlayerStategameModeSystemInitialized(bool val, GhostSerializerState serializerState)
    {
        PlayerStategameModeSystemInitialized = val?1u:0;
    }
    public void SetPlayerStategameModeSystemInitialized(bool val)
    {
        PlayerStategameModeSystemInitialized = val?1u:0;
    }

    public void PredictDelta(uint tick, ref PlayerStateSnapshotData baseline1, ref PlayerStateSnapshotData baseline2)
    {
        var predictor = new GhostDeltaPredictor(tick, this.tick, baseline1.tick, baseline2.tick);
        PlayerStateplayerId = predictor.PredictInt(PlayerStateplayerId, baseline1.PlayerStateplayerId, baseline2.PlayerStateplayerId);
        PlayerStategameModeSystemInitialized = (uint)predictor.PredictInt((int)PlayerStategameModeSystemInitialized, (int)baseline1.PlayerStategameModeSystemInitialized, (int)baseline2.PlayerStategameModeSystemInitialized);
    }

    public void Serialize(int networkId, ref PlayerStateSnapshotData baseline, ref DataStreamWriter writer, NetworkCompressionModel compressionModel)
    {
        changeMask0 = (PlayerStateplayerId != baseline.PlayerStateplayerId) ? 1u : 0;
        changeMask0 |= (PlayerStategameModeSystemInitialized != baseline.PlayerStategameModeSystemInitialized) ? (1u<<1) : 0;
        writer.WritePackedUIntDelta(changeMask0, baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            writer.WritePackedIntDelta(PlayerStateplayerId, baseline.PlayerStateplayerId, compressionModel);
        if ((changeMask0 & (1 << 1)) != 0)
            writer.WritePackedUIntDelta(PlayerStategameModeSystemInitialized, baseline.PlayerStategameModeSystemInitialized, compressionModel);
    }

    public void Deserialize(uint tick, ref PlayerStateSnapshotData baseline, ref DataStreamReader reader,
        NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
        changeMask0 = reader.ReadPackedUIntDelta(baseline.changeMask0, compressionModel);
        if ((changeMask0 & (1 << 0)) != 0)
            PlayerStateplayerId = reader.ReadPackedIntDelta(baseline.PlayerStateplayerId, compressionModel);
        else
            PlayerStateplayerId = baseline.PlayerStateplayerId;
        if ((changeMask0 & (1 << 1)) != 0)
            PlayerStategameModeSystemInitialized = reader.ReadPackedUIntDelta(baseline.PlayerStategameModeSystemInitialized, compressionModel);
        else
            PlayerStategameModeSystemInitialized = baseline.PlayerStategameModeSystemInitialized;
    }
    public void Interpolate(ref PlayerStateSnapshotData target, float factor)
    {
    }
}
