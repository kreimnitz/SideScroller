using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public interface IClientNetworkedGameManager
{
    public long GameTick { get; }
    public void HandleIncomingMessages();
    public void AdvanceGamestate(double deltaS);
    public void SetMessenger(IClientSideMessenger messenger);
}

public class ClientNetworkedGameManager<TGameState> : IClientNetworkedGameManager where TGameState : IClientGameState
{
    private double _timeSinceLastPingS;
    private GameInput _lastSentInput;
    private long _lastSentInputGameTicks = -1;
    private long _minMessageIntervalTicks = 12 * TimeSpan.TicksPerMillisecond; 
    private IClientSideMessenger _clientSideMessenger;
    private SnapshotHistory<ServerInputSnapshot> _serverSnapshotHistory = new();
    private ClientInputBuffer _clientInputBuffer = new();
    public TGameState GameState { get; private set; }
    private TGameState _confirmedGameState;
    private GameInput[] _lastConfirmedInputs;
    private Stopwatch _gameTimer = new();
    public long GameTick => _gameTimer.ElapsedTicks;
    private int PlayerId => _clientSideMessenger.PlayerId;

    public ClientNetworkedGameManager(TGameState gameState, TGameState rollbackState)
    {
        GameState = gameState;
        _confirmedGameState = rollbackState;
    }

    public void SetMessenger(IClientSideMessenger messenger)
    {
        _clientSideMessenger = messenger;
    }

    public void AdvanceGamestate(double deltaS)
    {
        CheckSendPing(deltaS);
        var input = GetInput();
        var inputId = _clientInputBuffer.AddInput(input, deltaS);
        CheckSendGameInput(input, inputId);
        HandleIncomingMessages();
        RequestMissingSnapshots();
        UpdateGameStates(input, deltaS);
    }

    public void HandleIncomingMessages()
    {
        foreach (var message in _clientSideMessenger.PollMessagesFromServer())
        {
            if (message.Type == MessageType.ServerGameInput)
            {
                var serverSnapshot = message.Data.ToServerGameInputSnapshot();
                _serverSnapshotHistory.AddSnapshot(serverSnapshot);
            }
            if (message.Type == MessageType.Start)
            {
                var startMessage = message.Data.ToStartGameData();
                SetPlayerCount(startMessage.PlayerCount);
                _gameTimer.Start();
            }
        }
    }

    private void CheckSendPing(double deltaS)
    {
        if (_timeSinceLastPingS > 1)
        {
            _clientSideMessenger.SendPing();
            _timeSinceLastPingS = 0;
        }
        else
        {
            _timeSinceLastPingS += deltaS;
        }
    }

    private void CheckSendGameInput(GameInput input, int id)
    {
        var ticksSinceLastSend = GameTick - _lastSentInputGameTicks;
        if (ticksSinceLastSend < _minMessageIntervalTicks && input.Equals(_lastSentInput))
        {
            return;
        }
        _lastSentInput = input;
        _lastSentInputGameTicks = GameTick;
        var message = Message.CreateClientInputSnapshotMessage(input, id);
        _clientSideMessenger.SendMessage(message);
    }

    private void UpdateGameStates(GameInput input, double deltaS)
    {
        var serverSnapshots = _serverSnapshotHistory.PopValidSnapshots();
        if (serverSnapshots.Any())
        {
            foreach (var serverInputSnapshot in serverSnapshots)
            {
                _confirmedGameState.ApplyInput(serverInputSnapshot.GameInputs, serverInputSnapshot.DeltaS, false);
                _lastConfirmedInputs = serverInputSnapshot.GameInputs;
            }
            var lastConfirmedLocalId = serverSnapshots.Last().LastProcessedIds[PlayerId];
            UpdateConfirmedStateAndApplyLocalInputBuffer(lastConfirmedLocalId);
        }
        else
        {
            _lastConfirmedInputs[_clientSideMessenger.PlayerId] = input;
            GameState.ApplyInput(_lastConfirmedInputs, deltaS, true);
        }
    }

    private void SetPlayerCount(int playerCount)
    {
        _lastConfirmedInputs = new GameInput[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            _lastConfirmedInputs[i] = new GameInput();
        }
    }

    private void UpdateConfirmedStateAndApplyLocalInputBuffer(int lastConfirmedLocalId)
    {
        GameState.CopyFrom(_confirmedGameState);
        _clientInputBuffer.TrimIdAndBefore(lastConfirmedLocalId);
        foreach (var snapshot in _clientInputBuffer.Snapshots)
        {
            _lastConfirmedInputs[_clientSideMessenger.PlayerId] = snapshot.GameInput;
            GameState.ApplyInput(_lastConfirmedInputs, snapshot.DeltaS, true);
        }
    }

    private void RequestMissingSnapshots()
    {
        var missingIds = _serverSnapshotHistory.MissingSnapshots.ToList();
        if (missingIds.Count != 0)
        {
            var message = Message.CreateSnapshotRequestMessage(missingIds);
            _clientSideMessenger.SendMessage(message);
        }
    }

    private GameInput GetInput()
    {
        var input = new GameInput();
        foreach (var key in _keysToCheck)
        {
            if (Input.IsKeyPressed(key))
            {
                input.PressedKeys.Add(key);
            }
        }
        return input;
    }

    private HashSet<Key> _keysToCheck = new()
    {
        Key.Left,
        Key.Right,
        Key.Space,
        Key.X,
        Key.Z
    };
}