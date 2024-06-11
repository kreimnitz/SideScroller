using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public interface IServerSideMessenger
{
    void SendMessageToClient(Message message);

    List<Message> PollMessagesFromClient();
}

public interface IClientSideMessenger
{
    long PingMs { get; }

    bool Connected { get; }

    int PlayerId { get; }

    bool AttemptConnection();

    void SendPing();

    void SendMessage(Message message);

    List<Message> PollMessagesFromServer();
}

public class ServerSideRemoteClientMessenger : IServerSideMessenger
{
    private PacketPeerUdp _peer;

    public ServerSideRemoteClientMessenger(PacketPeerUdp peer)
    {
        _peer = peer;
    }

    public List<Message> PollMessagesFromClient()
    {
        var messageList = new List<Message>();
        while (_peer.GetAvailablePacketCount() > 0)
        {
            var message = _peer.GetPacket().ToMessage();
            if (message == null)
            {
                continue;
            }
            messageList.Add(message);
        }
        return messageList;
    }

    public void SendMessageToClient(Message message)
    {
        _peer.PutPacket(message.ToByteArray());
    }
}

public class LocalClientMessenger : IServerSideMessenger, IClientSideMessenger
{
    private PingCalculator _pingCalculator = new();
    private Queue<Message> _messagesForServer = new();
    private Queue<Message> _messagesForClient = new();

    public long PingMs { get ; private set; }

    public bool Connected => true;

    public int PlayerId => 0;

    public bool AttemptConnection() => true;

    public LocalClientMessenger()
    {
    }

    public List<Message> PollMessagesFromClient()
    {
        var messages = _messagesForServer.ToList();
        _messagesForServer.Clear();
        return messages;
    }

    public List<Message> PollMessagesFromServer()
    {
        var messages = _messagesForClient.ToList();
        _messagesForClient.Clear();
        foreach (var message in messages)
        {
            if (message.Type == MessageType.Pong)
            {
                PingMs = _pingCalculator.GetPingTime(message);
            }
        }
        return messages;
    }

    public void SendMessage(Message message)
    {
        _messagesForServer.Enqueue(message);
    }

    public void SendMessageToClient(Message message)
    {
        _messagesForClient.Enqueue(message);
    }

    public void SendPing()
    {
        var ping = _pingCalculator.MakePing();
        _messagesForServer.Enqueue(ping);
    }
}