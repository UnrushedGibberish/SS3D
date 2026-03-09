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

		BuildMainMenu();

		// Subscribe to network manager/autoload lifecycle
		if (NetworkManager.Instance != null)
		{
			NetworkManager.Instance.ConnectedToServerEvent += OnConnectedToServer;
			NetworkManager.Instance.ConnectionFailedEvent += OnConnectionFailed;
			NetworkManager.Instance.HostStartedEvent += OnHostStarted;
			NetworkManager.Instance.ServerDisconnectedEvent += OnServerDisconnected;
		}
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
		_mainMenu?.SetStatus("Hosting: server started.");
		//CleanupMainMenu();
	}

	private void OnConnectedToServer()
	{
		_mainMenu?.SetStatus("Connected to server.");
		//CleanupMainMenu();
	}

	private void OnConnectionFailed()
	{
		_mainMenu?.SetStatus("Connection failed.");
	}

	private void OnServerDisconnected()
	{
		_mainMenu?.SetStatus("Server disconnected.");
		//CleanupMainMenu();
		//BuildMainMenu();
	}

	private void BuildMainMenu()
	{
		if (MainMenuScene == null || GUIRoot == null)
		{
			GD.PrintErr("Cannot build main menu: missing references.");
			return;
		}

		var mainMenu = MainMenuScene.Instantiate();
		GUIRoot.AddChild(mainMenu);
		_mainMenu = mainMenu as MainMenu;
	}

	private void CleanupMainMenu()
	{
		_mainMenu?.QueueFree();
		_mainMenu = null;
	}
}
