public class ServerGameState
{
    private GameInput[] _lastNonNullInputs;
    private int _nextServerMessageId = 0;
    private int _playerCount;

    public ServerGameState(int playerCount)
    {
        _playerCount = playerCount;
        _lastNonNullInputs = new GameInput[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            _lastNonNullInputs[i] = new();
        }
    }

    public ServerInputSnapshot ProcessClientInputs(ClientInputSnapshot[] clientInputs, double deltaS)
    {
        var serverInputSnapshot = new ServerInputSnapshot()
        {
            Id = _nextServerMessageId++,
            DeltaS = deltaS,
            GameInputs = new GameInput[clientInputs.Length],
            LastProcessedIds = new int[clientInputs.Length]
        };

        for (int i = 0; i < _playerCount; i++)
        {
            var playerInput = clientInputs[i].Input;
            if (playerInput == null)
            {
                playerInput = _lastNonNullInputs[i];
            }
            else
            {
                _lastNonNullInputs[i] = playerInput;
            }
            serverInputSnapshot.GameInputs[i] = playerInput;
            serverInputSnapshot.LastProcessedIds[i] = clientInputs[i].Id;
        }

        return serverInputSnapshot;
    }
}