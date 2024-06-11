using System.Collections.Generic;
using Godot;

public class RemoteClient : IClientSideMessenger
{
    private PingCalculator _pingCalculator = new();
    private PacketPeerUdp _client = new();
    public bool Connected { get; private set; }
    public int PlayerId { get; private set; } = -1;
    public long PingMs { get; private set; } = -1;

    public RemoteClient(string ip)
    {
        _client.ConnectToHost(ip, 11000);
    }

    public bool AttemptConnection()
    {
        if (Connected)
        {
            return true;
        }

        if (_client.GetAvailablePacketCount() > 0)
        {
            Connected = true;
            ProcessPacket(_client.GetPacket());
        }
        if (!Connected && PingMs == -1)
        {
            SendPing();
        }
        return Connected;
    }

    public List<Message> PollMessagesFromServer()
    {
        var messages = new List<Message>();
        while (_client.GetAvailablePacketCount() > 0)
        {
            var message = ProcessPacket(_client.GetPacket());
            if (message != null)
            {
                messages.Add(message);
            }
        }
        return messages;
    }

    private Message ProcessPacket(byte[] packet)
    {
        var message = packet.ToMessage();
        if (message == null)
        {
            return null;
        }

        if (message.Type == MessageType.Pong)
        {
            var data = message.Data.ToPongData();
            PlayerId = data.PlayerId;
            PingMs = _pingCalculator.GetPingTime(message);
        }
        return message;
    }

    public void SendMessage(Message message)
    {
        _client.PutPacket(message.ToByteArray());
    }

    public void SendPing()
    {
        var ping = _pingCalculator.MakePing();
        SendMessage(ping);
    }
}