using System;
using System.Collections.Generic;
using Godot;
using ProtoBuf;

[ProtoContract]
public class GameInput
{
    [ProtoMember(1)]
    public HashSet<Key> PressedKeys { get; set; } = new();

    public bool Equals(GameInput other)
    {
        if (other is null)
        {
            return false;
        }
        return PressedKeys.SetEquals(other.PressedKeys);
    }

    public void CombineWith(GameInput other)
    {
        foreach (var key in other.PressedKeys)
        {
            PressedKeys.Add(key);
        }
    }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}

public interface ISequentialSnapshot
{
    int Id { get; set; }
}

[ProtoContract]
public class ClientInputSnapshot : ISequentialSnapshot
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public GameInput Input { get; set; }

    public void CombineWith(ClientInputSnapshot other)
    {
        Id = Math.Max(Id, other.Id);
        Input.CombineWith(other.Input);
    }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}

[ProtoContract]
public class ServerInputSnapshot : ISequentialSnapshot
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public double DeltaS { get; set; }

    [ProtoMember(3)]
    public GameInput[] GameInputs { get; set; }

    [ProtoMember(4)]
    public int[] LastProcessedIds { get; set; }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}

[ProtoContract]
public class SnapshotRequest
{
    [ProtoMember(1)]
    public List<int> Ids { get; set; }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}