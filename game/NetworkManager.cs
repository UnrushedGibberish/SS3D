using Godot;
using System;
using System.Collections.Generic;

namespace SS3D;

/// <summary>
/// Autoloaded networking manager. Owns ENet peer and exposes the runtime networking API directly.
/// This replaces the previous thin manager + service split by keeping networking behavior here.
/// </summary>
public partial class NetworkManager : Node
{
	public static NetworkManager? Instance {get; private set; }

	[Signal] public delegate void ConnectedToServerEventHandler();
	[Signal] public delegate void ConnectionFailedEventHandler();
	[Signal] public delegate void PeerConnectedEventHandler(long id);
	[Signal] public delegate void PeerDisconnectedEventHandler(long id);
	[Signal] public delegate void ServerDisconnectedEventHandler();
	[Signal] public delegate void HostStartedEventHandler();

	public event Action? ConnectedToServerEvent;
	public event Action? ConnectionFailedEvent;
	public event Action<long>? PeerConnectedEvent;
	public event Action<long>? PeerDisconnectedEvent;
	public event Action? ServerDisconnectedEvent;
	public event Action? HostStartedEvent;

	public bool IsServer { get; private set; }
	public bool IsClient { get; private set; }
	public new bool IsConnected => IsServer || IsClient;

	public IReadOnlyCollection<long> ConnectedPeerIds
	{
		get
		{
			var list = new List<long>();
			return list;
		}
	}

	public override void _Ready()
	{
		Instance = this;
		
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
	}

	public Error Host(ushort port, int maxClients = 16)
	{
		Stop();

		var peer = new ENetMultiplayerPeer();
		var err = peer.CreateServer(port, maxClients);
		if (err != Error.Ok)
		{
			return err;
		}

		Multiplayer.MultiplayerPeer = peer;
		IsServer = true;
		IsClient = false;
		EmitSignal(SignalName.HostStarted);
		HostStartedEvent?.Invoke();
		return Error.Ok;
	}

	public Error Join(string host, ushort port)
	{
		Stop();

		var peer = new ENetMultiplayerPeer();
		var err = peer.CreateClient(host, port);
		if (err != Error.Ok)
		{
			return err;
		}

		Multiplayer.MultiplayerPeer = peer;
		IsServer = false;
		IsClient = true;
		return Error.Ok;
	}

	public void Stop()
	{
		Multiplayer.MultiplayerPeer = null;
		IsServer = false;
		IsClient = false;
	}

	private void OnConnectedToServer()
	{
		EmitSignal(SignalName.ConnectedToServer);
		ConnectedToServerEvent?.Invoke();
	}

	private void OnConnectionFailed()
	{
		EmitSignal(SignalName.ConnectionFailed);
		ConnectionFailedEvent?.Invoke();
	}

	private void OnPeerConnected(long id)
	{
		EmitSignal(SignalName.PeerConnected, id);
		PeerConnectedEvent?.Invoke(id);
	}

	private void OnPeerDisconnected(long id)
	{
		EmitSignal(SignalName.PeerDisconnected, id);
		PeerDisconnectedEvent?.Invoke(id);
	}

	private void OnServerDisconnected()
	{
		EmitSignal(SignalName.ServerDisconnected);
		ServerDisconnectedEvent?.Invoke();
	}
}
