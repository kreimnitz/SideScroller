using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;

[ProtoContract]
public class Message
{
    public static Message CreatePing(int id)
    {
        var data = new PingData()
        {
            Id = id
        };
        return new Message()
        {
            Type = MessageType.Ping,
            Data = data.ToByteArray()
        };
    }

    public static Message CreatePong(Message ping, int playerId)
    {
        var pingData = ping.Data.ToPingData();
        var data = new PongData()
        {
            Id = pingData.Id,
            PlayerId = playerId
        };
        return new Message()
        {
            Type = MessageType.Pong,
            Data = data.ToByteArray()
        };
    }

    public static Message CreateClientInputSnapshotMessage(GameInput snapshot, int id)
    {
        var data = new ClientInputSnapshot()
        {
            Id = id,
            Input = snapshot
        };
        return new Message()
        {
            Type = MessageType.ClientGameInput,
            Data = data.ToByteArray()
        };
    }

    public static Message CreateServerSnapshotMessage(ServerInputSnapshot snapshot)
    {
        return new Message()
        {
            Type = MessageType.ServerGameInput,
            Data = snapshot.ToByteArray()
        };
    }

    public static Message CreateStartGameMessage(int playerCount)
    {
        var data = new StartGameData() { PlayerCount = playerCount };
        return new Message()
        {
            Type = MessageType.Start,
            Data = data.ToByteArray()
        };
    }

    public static Message CreateSnapshotRequestMessage(List<int> ids)
    {
        var data = new SnapshotRequest() { Ids = ids };
        return new Message()
        {
            Type = MessageType.SnapshotRequest,
            Data = data.ToByteArray()
        };
    }

    [ProtoMember(1)]
    public MessageType Type { get; set; }

    [ProtoMember(2)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}

public enum MessageType
{
    Ping,
    Pong,
    Start,
    ClientGameInput,
    ServerGameInput,
    SnapshotRequest
}

public static class SerializationUtilities
{
    public static byte[] ToByteArray<T>(T toSerialize)
    {
        using (var ms = new MemoryStream())
        {
            Serializer.Serialize(ms, toSerialize);
            return ms.ToArray();
        }
    }

    public static T FromByteArray<T>(byte[] bytes)
    {
        using (var ms = new MemoryStream(bytes))
        {
            return Serializer.Deserialize<T>(ms);
        }
    }

    public static T TryFromByteArray<T>(this byte[] bytes)
    {
        try
        {
            return FromByteArray<T>(bytes);
        }
        catch
        {
            return default;
        }
    }

    public static Message ToMessage(this byte[] bytes)
    {
        return TryFromByteArray<Message>(bytes);
    }

    public static ClientInputSnapshot ToClientInputSnapshot(this byte[] bytes)
    {
        return TryFromByteArray<ClientInputSnapshot>(bytes);
    }

    public static ServerInputSnapshot ToServerGameInputSnapshot(this byte[] bytes)
    {
        return TryFromByteArray<ServerInputSnapshot>(bytes);
    }

    public static PingData ToPingData(this byte[] bytes)
    {
        return TryFromByteArray<PingData>(bytes);
    }

    public static PongData ToPongData(this byte[] bytes)
    {
        return TryFromByteArray<PongData>(bytes);
    }

    public static StartGameData ToStartGameData(this byte[] bytes)
    {
        return TryFromByteArray<StartGameData>(bytes);
    }

    public static SnapshotRequest ToSnapshotRequest(this byte[] bytes)
    {
        return TryFromByteArray<SnapshotRequest>(bytes);
    }
}