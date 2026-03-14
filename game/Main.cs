using Godot;
using System;

namespace SS3D;

public partial class Main : Node
{
	[ExportGroup("Node References")]
	[Export] public Node3D? WorldRoot { get; set; }
	[Export] public CanvasLayer? GUIRoot { get; set; }

	[ExportGroup("Scene References")]
	[Export] public PackedScene? MainMenuScene { get; set; }
	[Export] public PackedScene? PlayerScene { get; set; }
	[Export] public PackedScene? GameScene { get; set; }

	private MainMenu? _mainMenu;
	private Map? _gameWorld;
	private Player? _player;

	public override void _Ready()
	{
		if (WorldRoot == null || GUIRoot == null)
		{
			GD.PrintErr("Node references are missing");
			return;
		}

		if (MainMenuScene == null || PlayerScene == null || GameScene == null)
		{
			GD.PrintErr("Scene references are missing");
			return;
		}
		
		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.ConnectedToServerEvent += OnConnectedToServer;
			NetworkManager.Instance.ConnectionFailedEvent += OnConnectionFailed;
			NetworkManager.Instance.HostStartedEvent += OnHostStarted;
			NetworkManager.Instance.ServerDisconnectedEvent += OnServerDisconnected;
		}

		LoadMainMenu();
	}

	public override void _ExitTree()
	{
		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.ConnectedToServerEvent -= OnConnectedToServer;
			NetworkManager.Instance.ConnectionFailedEvent -= OnConnectionFailed;
			NetworkManager.Instance.HostStartedEvent -= OnHostStarted;
			NetworkManager.Instance.ServerDisconnectedEvent -= OnServerDisconnected;
		}
	}

	private void OnHostStarted()
	{
		UnloadMainMenu();
		LoadGame();
	}

	private void OnConnectedToServer()
	{
		UnloadMainMenu();
		LoadGame();
	}

	private void OnConnectionFailed()
	{
		_mainMenu?.SetStatus("Connection failed.");
	}

	private void OnServerDisconnected()
	{
		UnloadGame();
		UnloadMainMenu();
		LoadMainMenu();
		_mainMenu?.SetStatus("Server disconnected.");
	}

	private void LoadMainMenu()
	{
		var mainMenu = MainMenuScene.Instantiate();
		GUIRoot.AddChild(mainMenu);
		_mainMenu = mainMenu as MainMenu;
	}

	private void UnloadMainMenu()
	{
		_mainMenu?.QueueFree();
		_mainMenu = null;
	}

	private void LoadGame()
	{
		UnloadGame();

		var gameWorld = GameScene.Instantiate<Map>();
		WorldRoot.AddChild(gameWorld);
		_gameWorld = gameWorld;

		var player = PlayerScene.Instantiate<Player>();
		WorldRoot.AddChild(player);
		player.GlobalPosition = _gameWorld.GetNextSpawnPosition();
		_player = player;
	}

	private void UnloadGame()
	{
		_gameWorld?.QueueFree();
		_gameWorld = null;

		_player?.QueueFree();
		_player = null;
	}
}
