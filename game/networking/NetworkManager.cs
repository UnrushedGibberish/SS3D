using Godot;
using System;
using System.Collections.Generic;

namespace SS3D.Networking;

/// <summary>
/// Autoloaded networking manager. Owns ENet peer and exposes the runtime networking API directly.
/// </summary>
public partial class NetworkManager : Node
{
	/// <summary>
	/// How often the server sends pings to clients (in seconds).
	/// </summary>
	private const double PingIntervalSeconds = 1.0;

	public static NetworkManager Instance { get; private set; } = null!;
	
	[Signal] public delegate void HostStartedEventHandler();
	[Signal] public delegate void ConnectedToServerEventHandler();
	[Signal] public delegate void DisconnectedFromServerEventHandler();
	[Signal] public delegate void ConnectionFailedEventHandler();
	[Signal] public delegate void PlayerJoinedEventHandler(long peerId);
	[Signal] public delegate void PlayerLeftEventHandler(long peerId);

	public bool IsServer { get; private set; }
	public bool IsClient { get; private set; }
	public bool HasConnection => IsServer || IsClient;
	public long LocalPeerId => Multiplayer.GetUniqueId();

	public Dictionary<long, PeerInfo> Players { get; private set; } = new Dictionary<long, PeerInfo>();

	/// <summary>
	/// Client-side RTT to the server.
	/// </summary>
	public double ClientRttMs { get; private set; }
	private double _pingTimer = 0.0;
	private ulong _clientPingSentTime = 0;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ServerDisconnected += OnDisconnectedFromServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
	}

	public override void _ExitTree()
	{
		Multiplayer.ConnectedToServer -= OnConnectedToServer;
		Multiplayer.ServerDisconnected -= OnDisconnectedFromServer;
		Multiplayer.ConnectionFailed -= OnConnectionFailed;
		Multiplayer.PeerConnected -= OnPeerConnected;
		Multiplayer.PeerDisconnected -= OnPeerDisconnected;
	}

	public Error CreateHost(int port, int maxClients = 16)
	{
		Stop();

		var peer = new ENetMultiplayerPeer();
		var error = peer.CreateServer(port, maxClients);
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to create server!");
			return error;
		}
		
		Multiplayer.MultiplayerPeer = peer;
		IsServer = true;
		IsClient = false;

		var localPeerInfo = new PeerInfo
		{
			Id = LocalPeerId,
			Name = "Host_Player",
			IsRegistered = true,
		};
		Players[LocalPeerId] = localPeerInfo;

		EmitSignal(SignalName.HostStarted);
		return Error.Ok;
	}

	public Error CreateClient(string address, int port)
	{
		Stop();

		var peer = new ENetMultiplayerPeer();
		var error = peer.CreateClient(address, port);
		if (error != Error.Ok)
		{
			GD.PrintErr("Failed to create client!");
			return error;
		}

		Multiplayer.MultiplayerPeer = peer;
		IsServer = false;
		IsClient = true;

		var localPeerInfo = new PeerInfo
		{
			Id = LocalPeerId,
			Name = $"Player_{LocalPeerId}",
			IsRegistered = false,
		};
		Players[LocalPeerId] = localPeerInfo;

		return Error.Ok;
	}

	public void Stop()
	{
		ClearConnection();
	}

	private void ClearConnection()
	{
		Multiplayer.MultiplayerPeer = null;
		IsServer = false;
		IsClient = false;
		ClientRttMs = 0;
		Players.Clear();
	}

	#region MultiplayerApi Callbacks

	private void OnConnectedToServer()
	{
		if (!IsClient)
		{
			GD.PrintErr($"[{LocalPeerId}]: ConnectedToServer callback invoked but this instance is not a client!");
			return;
		}

		GD.Print($"[{LocalPeerId}]: Successfully connected to server!");

		EmitSignal(SignalName.ConnectedToServer);
	}

	private void OnDisconnectedFromServer()
	{
		if (!IsClient)
		{
			GD.PrintErr("DisconnectedFromServer callback invoked but this instance is not a client!");
			return;
		}

		GD.Print("Server disconnected!");
		
		ClearConnection();

		EmitSignal(SignalName.DisconnectedFromServer);
	}

	private void OnConnectionFailed()
	{
		if (!IsClient)
		{
			GD.PrintErr($"[{LocalPeerId}]: ConnectionFailed callback invoked but this instance is not a client!");
			return;
		}

		GD.PrintErr($"[{LocalPeerId}]: Failed to connect to server!");
		
		ClearConnection();

		EmitSignal(SignalName.ConnectionFailed);
	}

	private void OnPeerConnected(long peerId)
	{
		GD.Print($"[{LocalPeerId}]: Peer connected: {peerId}, awaiting registration...");

		var info = new PeerInfo
		{
			Id = peerId,
			Name = string.Empty,
			IsRegistered = false,
		};

		Players[peerId] = info;
		
		var myName = $"Player_{LocalPeerId}"; 
		Rpc(nameof(RegisterPlayerInfo), myName);

		EmitSignal(SignalName.PlayerJoined, peerId);
	}

	private void OnPeerDisconnected(long peerId)
	{
		GD.Print($"[{LocalPeerId}]: Peer disconnected: {peerId}");
		Players.Remove(peerId);

		EmitSignal(SignalName.PlayerLeft, peerId);
	}

	#endregion // MultiplayerApi Callbacks

	#region Player Registration

	/// <summary>
	/// Called on the SERVER when a client sends their info.
	/// </summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RegisterPlayerInfo(string playerName)
	{
		var peerId = Multiplayer.GetRemoteSenderId();
		if (!Players.TryGetValue(peerId, out var info))
		{
			GD.PrintErr($"[{LocalPeerId}]: Received client info from unknown peer: {peerId}");
			return;
		}

		info.Name = playerName;
		info.IsRegistered = true;
		Players[peerId] = info;

		GD.Print($"[{LocalPeerId}]: Player joined: {playerName}");
	}

	#endregion // Player Registration

	#region Ping/Pong

	public override void _PhysicsProcess(double delta)
	{
		if (IsServer)
		{
			ServerPingUpdate(delta);
		}
		else if (IsClient)
		{
			ClientPingUpdate(delta);
		}
	}

	/// <summary>
	/// Called on the SERVER every PingIntervalSeconds to ping all registered clients and measure RTT.
	/// </summary>
	/// <param name="delta">Time elapsed since the last frame, in seconds.</param>
	private void ServerPingUpdate(double delta)
	{
		_pingTimer += delta;
		if (_pingTimer < PingIntervalSeconds)
		{
			return;
		}
		_pingTimer = 0;

		var currentTime = Time.GetTicksMsec();

		foreach (var kvp in Players)
		{
			if (!kvp.Value.IsRegistered || kvp.Key == LocalPeerId) // Don't ping unregistered peers or self
			{
				continue;
			}

			var info = kvp.Value;
			info.LastPingSentTime = currentTime;
			Players[kvp.Key] = info;

			RpcId(kvp.Key, nameof(ClientReceivePing), currentTime);
		}
	}
	
	/// <summary>
	/// Called on the CLIENT when server pings them.
	/// </summary>
	/// <param name="serverTimestamp">The timestamp sent by the server.</param>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void ClientReceivePing(ulong serverTimestamp)
	{
		// Immediately respond with pong
		RpcId(1, nameof(ServerReceivePong), serverTimestamp);
	}
	
	/// <summary>
	/// Called on the SERVER when client responds to their ping.
	/// </summary>
	/// <param name="originalTimestamp">The timestamp sent by the client.</param>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void ServerReceivePong(ulong originalTimestamp)
	{
		if (!IsServer)
		{
			return;
		}

		var senderId = Multiplayer.GetRemoteSenderId();
		var currentTime = Time.GetTicksMsec();
		var rtt = currentTime - originalTimestamp;

		if (Players.TryGetValue(senderId, out var info))
		{
			info.RttMs = rtt;
			info.LastPongReceivedTime = currentTime;
			Players[senderId] = info;
		}
	}

	/// <summary>
	/// Called on the CLIENT every PingIntervalSeconds to ping the server and measure RTT.
	/// </summary>
	/// <param name="delta">Time elapsed since the last frame, in seconds.</param>
	private void ClientPingUpdate(double delta)
	{
		_pingTimer += delta;
		if (_pingTimer < PingIntervalSeconds)
		{
			return;
		}
		_pingTimer = 0;

		_clientPingSentTime = Time.GetTicksMsec();
		RpcId(1, nameof(ServerReceivePing), _clientPingSentTime);
	}

	/// <summary>
	/// Called on the SERVER when client pings them.
	/// </summary>
	/// <param name="clientTimestamp">The timestamp sent by the client.</param>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void ServerReceivePing(ulong clientTimestamp)
	{
		if (!IsServer)
		{
			return;
		}

		var senderId = Multiplayer.GetRemoteSenderId();
		// Echo back so client can calculate their RTT
		RpcId(senderId, nameof(ClientReceivePong), clientTimestamp);
	}
	
	/// <summary>
	/// Called on the CLIENT when server responds to their ping.
	/// </summary>
	/// <param name="originalTimestamp">The timestamp sent by the client.</param>
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void ClientReceivePong(ulong originalTimestamp)
	{
		var currentTime = Time.GetTicksMsec();
		ClientRttMs = currentTime - originalTimestamp;
	}

	#endregion // Ping/Pong

	#region Utility Methods

	/// <summary>
	/// Get the RTT for a specific peer (server-side only).
	/// </summary>
	public double GetPeerRtt(long peerId)
	{
		if (Players.TryGetValue(peerId, out var info))
		{
			return info.RttMs;
		}
		return -1;
	}

	/// <summary>
	/// Check if a peer is fully registered.
	/// </summary>
	public bool IsPeerRegistered(long peerId)
	{
		return Players.TryGetValue(peerId, out var info) && info.IsRegistered;
	}

	#endregion // Utility Methods
}
