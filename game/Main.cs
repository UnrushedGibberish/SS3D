using Godot;
using SS3D.Networking;
using System;

namespace SS3D;

public partial class Main : Node
{
	[ExportGroup("Node References")]
	[Export] public Node3D? WorldRoot { get; set; }
	[Export] public CanvasLayer? GUIRoot { get; set; }
	[Export] public MultiplayerSpawner? Spawner { get; set; }

	[ExportGroup("Scene References")]
	[Export] public PackedScene? MainMenuScene { get; set; }
	[Export] public PackedScene? PlayerScene { get; set; }
	[Export] public PackedScene? GameScene { get; set; }

	public override void _Ready()
	{
		if (WorldRoot == null || GUIRoot == null || Spawner == null)
		{
			GD.PrintErr("Node references are missing");
			return;
		}

		if (MainMenuScene == null || PlayerScene == null || GameScene == null)
		{
			GD.PrintErr("Scene references are missing");
			return;
		}

		NetworkManager.Instance.HostStarted += OnHostStarted;
		NetworkManager.Instance.ConnectedToServer += OnConnectedToServer;
		NetworkManager.Instance.DisconnectedFromServer += OnDisconnectedFromServer;
		NetworkManager.Instance.ConnectionFailed += OnConnectionFailed;
		NetworkManager.Instance.PlayerJoined += OnPlayerJoined;
		NetworkManager.Instance.PlayerLeft += OnPlayerLeft;

		Spawner.Spawned += OnSpawn;

		LoadMainMenu();
	}

	private void OnSpawn(Node node)
	{
		GD.Print($"Node spawned: {node.Name}");
	}

	public override void _ExitTree()
	{
		NetworkManager.Instance.HostStarted -= OnHostStarted;
		NetworkManager.Instance.ConnectedToServer -= OnConnectedToServer;
		NetworkManager.Instance.DisconnectedFromServer -= OnDisconnectedFromServer;
		NetworkManager.Instance.ConnectionFailed -= OnConnectionFailed;
		NetworkManager.Instance.PlayerJoined -= OnPlayerJoined;
		NetworkManager.Instance.PlayerLeft -= OnPlayerLeft;
	}

	private void OnHostStarted()
	{
		if (!NetworkManager.Instance.IsServer)
		{
			GD.PrintErr("HostStarted signal received but this instance is not a server.");
			return;
		}

		ClearGUI();
		ClearWorld();
		LoadGame();
		SpawnPlayer(NetworkManager.Instance.LocalPeerId);
	}

	private void OnConnectedToServer()
	{
		ClearGUI();
		ClearWorld();
		LoadGame();
	}

	private void OnConnectionFailed()
	{
		//_mainMenu?.SetStatus("Connection failed.");
	}

	private void OnPlayerJoined(long peerId)
	{
		if (!NetworkManager.Instance.IsServer)
		{
			return;
		}

		SpawnPlayer(peerId);
	}

	private void OnPlayerLeft(long peerId)
	{
		if (!NetworkManager.Instance.IsServer)
		{
			return;
		}

		var playerNode = WorldRoot?.GetNodeOrNull(peerId.ToString());
		playerNode?.QueueFree();
	}

	private void OnDisconnectedFromServer()
	{
		ClearWorld();
		ClearGUI();
		LoadMainMenu();
		//_mainMenu?.SetStatus("Server disconnected.");
	}

	private void LoadMainMenu()
	{
		var mainMenu = MainMenuScene?.Instantiate<MainMenu>();
		if (mainMenu != null)
		{
			GUIRoot?.AddChild(mainMenu);
		}
		else
		{
			GD.PrintErr("Failed to instantiate MainMenuScene.");
		}
	}

	private void LoadGame()
	{
		var gameWorld = GameScene?.Instantiate<Map>();
		if (gameWorld != null)
		{
			WorldRoot?.AddChild(gameWorld);
		}
		else
		{
			GD.PrintErr("Failed to instantiate GameScene.");
		}
	}

	private void SpawnPlayer(long peerId)
	{
		var player = PlayerScene?.Instantiate<Player>();
		if (player != null)
		{
			player.Name = peerId.ToString();
			WorldRoot?.AddChild(player);
		}
		else
		{
			GD.PrintErr("Failed to instantiate PlayerScene.");
		}
	}

	private void ClearGUI()
	{
		if (GUIRoot == null)
		{
			return;
		}
		
		foreach (Node child in GUIRoot.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void ClearWorld()
	{
		if (WorldRoot == null)
		{
			return;
		}

		foreach (Node child in WorldRoot.GetChildren())
		{
			if (child is MultiplayerSpawner)
			{
				continue;
			}
			child.QueueFree();
		}
	}
}
