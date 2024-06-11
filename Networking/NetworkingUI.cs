using Godot;

public partial class NetworkingUI : Control
{
	private bool _isHost;
	private const string IP = "173.174.103.71";
	private VBoxContainer _hostChoiceContainer;
	private Button _hostButton;
	private Button _joinButton;
	private VBoxContainer _hostGameStartContainer;
	private Label _remoteConnectionsLabel;
	private Button _startGameButton;
	private Label _label;
	private IClientSideMessenger _clientMessenger;
	private IClientNetworkedGameManager _gameManager;
	private Server _server;
	private NetworkingState State = NetworkingState.HostChoiceOption;

	private enum NetworkingState
	{
		HostChoiceOption,
		WaitingForConnection,
		WaitingForStart,
		Active,
	}

	public void SetManager(IClientNetworkedGameManager networkedGameManager)
	{
		_gameManager = networkedGameManager;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_hostChoiceContainer = GetNode<VBoxContainer>("HostChoiceContainer");
		_hostButton = _hostChoiceContainer.GetNode<Button>("HostButton");
		_joinButton = _hostChoiceContainer.GetNode<Button>("JoinButton");

		_hostGameStartContainer = GetNode<VBoxContainer>("HostGameStartContainer");
		_remoteConnectionsLabel = _hostGameStartContainer.GetNode<Label>("RemoteConnectionsLabel");
		_startGameButton = _hostGameStartContainer.GetNode<Button>("StartGameButton");

		_label = GetNode<Label>("Label");

		_hostGameStartContainer.Hide();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (State == NetworkingState.Active && _isHost)
		{
			_server?.ProcessNewMessages(delta);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (State == NetworkingState.HostChoiceOption)
		{
			ProcessHostChoice();
		}
		if (State == NetworkingState.WaitingForConnection)
		{
			ProcessWaitingForConnection();
		}
		if (State == NetworkingState.WaitingForStart)
        {
            ProcessWaitingForStart();
        }
        if (State == NetworkingState.Active)
		{
			_gameManager.AdvanceGamestate(delta);
			var pingInMs = _clientMessenger.PingMs;
			_label.Text = $"Ping: {pingInMs}ms";
		}
	}

    private void ProcessWaitingForStart()
    {
        if (_isHost)
        {
            var remoteConnectionCount = _server.CheckConnections();
            _remoteConnectionsLabel.Text = $"Players: {remoteConnectionCount}";
            if (_startGameButton.ButtonPressed)
            {
                _server.StartGame();
                _hostGameStartContainer.Hide();
				_label.Show();
                State = NetworkingState.Active;
            }
        }
        else
        {
            _label.Text = "Waiting for host...";
            _gameManager.HandleIncomingMessages();
            if (_gameManager.GameTick != 0)
            {
                State = NetworkingState.Active;
            }
        }
    }

    private void ProcessWaitingForConnection()
	{
		if (!_clientMessenger.Connected)
		{
			_clientMessenger.AttemptConnection();
		}
		else
		{
			State = NetworkingState.WaitingForStart;
		}
	}

	private void ProcessHostChoice()
	{
		var host = _hostButton.ButtonPressed;
		var join = _joinButton.ButtonPressed;
		if (!host && !join)
		{
			return;
		}

		if (host)
		{
			_isHost = true;
		}
		else if (join)
		{
			_isHost = false;
		}
		State = NetworkingState.WaitingForConnection;
		_hostChoiceContainer.Hide();

		if (_isHost)
		{
			_server = new();
			var localMessenger = new LocalClientMessenger();
			_server.AddServerSideMessenger(localMessenger);
			_clientMessenger = localMessenger;
			_hostGameStartContainer.Show();
		}
		else
		{
			_clientMessenger = new RemoteClient(IP);
			_label.Show();
			_label.Text = $"Connecting to {IP}...";
		}
		_gameManager.SetMessenger(_clientMessenger);
	}
}
