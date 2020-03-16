using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.NetCode;
using Unity.Sample.Core;

[BurstCompile]
public struct RpcInitializeMap : IRpcCommand
{
    public int MapId;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteInt(MapId);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        MapId = reader.ReadInt();
    }
    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcInitializeMap);
        rpcData.Deserialize(ref parameters.Reader);

        var ent = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, ent,
            new ActiveStateComponentData { MapId = rpcData.MapId});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class InitializeMapRpcCommandRequestSystem : RpcCommandRequestSystem<RpcInitializeMap>
{
}

[BurstCompile]
public struct RpcPlayerSetup : IRpcCommand
{
    //public NativeString64 PlayerName;
    public int CharacterType;
    //public short TeamId;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteInt(CharacterType);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        CharacterType = reader.ReadInt();
    }
    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcPlayerSetup);
        rpcData.Deserialize(ref parameters.Reader);

        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection,
            new PlayerSettingsComponent {CharacterType = rpcData.CharacterType});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class PlayerSetupRpcCommandRequestSystem : RpcCommandRequestSystem<RpcPlayerSetup>
{
}

[BurstCompile]
public struct RpcPlayerReady : IRpcCommand
{
    public void Serialize(ref DataStreamWriter writer)
    {
    }

    public void Deserialize(ref DataStreamReader reader)
    {
    }
    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, new PlayerReadyComponent());
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, parameters.Connection, new NetworkStreamInGame());
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class PlayerReadyRpcCommandRequestSystem : RpcCommandRequestSystem<RpcPlayerReady>
{
}

[BurstCompile]
public struct RpcRemoteCommand : IRpcCommand
{
    public NativeString64 Command;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUShort(Command.LengthInBytes);
        unsafe
        {
            fixed (byte* b = &Command.buffer.byte0000)
            {
                writer.WriteBytes(b, Command.LengthInBytes);
            }
        }
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        var msgLength = reader.ReadUShort();
        GameDebug.Assert(msgLength <= NativeString64.MaxLength);
        Command.LengthInBytes = msgLength;
        unsafe
        {
            fixed (byte* b = &Command.buffer.byte0000)
            {
                reader.ReadBytes(b, Command.LengthInBytes);
            }
        }
    }

    [BurstCompile]
    private static void InvokeExecute(ref RpcExecutor.Parameters parameters)
    {
        var rpcData = default(RpcRemoteCommand);
        rpcData.Deserialize(ref parameters.Reader);

        var req = parameters.CommandBuffer.CreateEntity(parameters.JobIndex);
        parameters.CommandBuffer.AddComponent(parameters.JobIndex, req, new IncomingRemoteCommandComponent{Command = rpcData.Command, Connection = parameters.Connection});
    }
    public PortableFunctionPointer<RpcExecutor.ExecuteDelegate> CompileExecute()
    {
        return new PortableFunctionPointer<RpcExecutor.ExecuteDelegate>(InvokeExecute);
    }
}
class RemoteCommandRpcCommandRequestSystem : RpcCommandRequestSystem<RpcRemoteCommand>
{
}

