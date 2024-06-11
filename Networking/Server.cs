using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Server
{
    private const int Port = 11000;
    private UdpServer _server = new();
    private List<IServerSideMessenger> _clientMessengers = new List<IServerSideMessenger>();
    private ServerGameState _gameState;
    private List<ServerInputSnapshot> _snapshots = new();
    public int PlayerCount => _clientMessengers.Count;

    public Server()
    {
        _server.Listen(Port);
    }

    public void StartGame()
    {
        var startMessage = Message.CreateStartGameMessage(PlayerCount);
        foreach (var client in _clientMessengers)
        {
            client.SendMessageToClient(startMessage);
        }
        _gameState = new ServerGameState(_clientMessengers.Count);
    }

    public int CheckConnections()
    {
        _server.Poll();
        while (_server.IsConnectionAvailable())
        {
            var peer = _server.TakeConnection();
            var message = peer.GetPacket().ToMessage();
            if (message?.Type == MessageType.Ping)
            {
                var remoteClient = new ServerSideRemoteClientMessenger(peer);
                _clientMessengers.Add(remoteClient);
                HandlePing(remoteClient, message);
            }
        }
        return _clientMessengers.Count();
    }

    public void ProcessNewMessages(double deltaS)
    {
        _server.Poll();
        var clientSnapshots = CheckMessages();
        var serverSnapshot = _gameState.ProcessClientInputs(clientSnapshots, deltaS);
        _snapshots.Add(serverSnapshot);
        var message = Message.CreateServerSnapshotMessage(serverSnapshot);
        foreach (var client in _clientMessengers)
        {
            client.SendMessageToClient(message);
        }
    }

    public void AddServerSideMessenger(IServerSideMessenger messenger)
    {
        _clientMessengers.Add(messenger);
    }

    private ClientInputSnapshot[] CheckMessages()
    {
        var clientInputs = new ClientInputSnapshot[_clientMessengers.Count];
        for (int i = 0; i < _clientMessengers.Count; i++)
        {
            var client = _clientMessengers[i];
            clientInputs[i] = new();
            clientInputs[i].Input = new();
            foreach (var message in client.PollMessagesFromClient())
            {
                if (message.Type == MessageType.Ping)
                {
                    HandlePing(client, message);
                }
                if (message.Type == MessageType.ClientGameInput)
                {
                    var snapshot = message.Data.ToClientInputSnapshot();
                    clientInputs[i].Id = Math.Max(clientInputs[i].Id, snapshot.Id);
                    clientInputs[i].Input.CombineWith(snapshot.Input);
                }
                if (message.Type == MessageType.SnapshotRequest)
                {
                    var request = message.Data.ToSnapshotRequest();
                    HandleSnapshotRequest(client, request.Ids);
                }
            }
        }
        return clientInputs;
    }

    private void HandleSnapshotRequest(IServerSideMessenger messenger, List<int> ids)
    {
        foreach (var id in ids)
        {
            var message = Message.CreateServerSnapshotMessage(_snapshots[id]);
            messenger.SendMessageToClient(message);
        }
    }

    private void HandlePing(IServerSideMessenger client, Message ping)
    {
        var index = _clientMessengers.IndexOf(client);
        client.SendMessageToClient(Message.CreatePong(ping, index));
    }
}