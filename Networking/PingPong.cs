using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProtoBuf;

[ProtoContract]
public class PingData
{
    [ProtoMember(1)]
    public int Id { get; set; }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}

[ProtoContract]
public class PongData
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public int PlayerId { get; set;}

    [ProtoMember(3)]
    public long StartTick { get; set; }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}


public class PingCalculator
{
    private int _nextPingId = 0;
    private Stopwatch _timer = new();
    private Dictionary<int, long> _pingTickstamps = new();
    private long _lastPingTime = -1;

    public Message MakePing()
    {
        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
        var pingId = _nextPingId++;
        _pingTickstamps.Add(pingId, _timer.ElapsedTicks);
        return Message.CreatePing(pingId);
    }

    public long GetPingTime(Message pong)
    {
        var pongTickstamp = _timer.ElapsedTicks;
        var pongData = pong.Data.ToPongData();
        if (_pingTickstamps.TryGetValue(pongData.Id, out long pingTickstamp))
        {
            _pingTickstamps.Remove(pongData.Id);
            _lastPingTime = (pongTickstamp - pingTickstamp) / TimeSpan.TicksPerMillisecond;
        }
        return _lastPingTime;
    }
}

[ProtoContract]
public class StartGameData
{
    [ProtoMember(1)]
    public int PlayerCount { get; set; }

    public byte[] ToByteArray()
    {
        return SerializationUtilities.ToByteArray(this);
    }
}