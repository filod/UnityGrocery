using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Mathematics;

public struct platformSnapshotData : ISnapshotData<platformSnapshotData>
{
    public uint tick;

    public uint Tick => tick;

    public void PredictDelta(uint tick, ref platformSnapshotData baseline1, ref platformSnapshotData baseline2)
    {
        var predictor = new GhostDeltaPredictor(tick, this.tick, baseline1.tick, baseline2.tick);
    }

    public void Serialize(int networkId, ref platformSnapshotData baseline, ref DataStreamWriter writer, NetworkCompressionModel compressionModel)
    {
    }

    public void Deserialize(uint tick, ref platformSnapshotData baseline, ref DataStreamReader reader,
        NetworkCompressionModel compressionModel)
    {
        this.tick = tick;
    }
    public void Interpolate(ref platformSnapshotData target, float factor)
    {
    }
}
