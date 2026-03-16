namespace SS3D.Networking;

/// <summary>
/// Represents information about a connected player/peer in the multiplayer session.
/// </summary>
public struct PeerInfo
{
	/// <summary>
	/// Unique identifier for the peer, assigned by Godot's multiplayer API.
	/// </summary>
	public long Id { get; set; }

	/// <summary>
	/// Player's display name, set during registration.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Whether this peer has completed registration.
	/// </summary>
	public bool IsRegistered { get; set; }

	/// <summary>
	/// Round-trip time in milliseconds, updated by ping/pong mechanism.
	/// </summary>
	public double RttMs { get; set; }

	/// <summary>
	/// Timestamp of the last ping sent to this peer (server-side tracking).
	/// </summary>
	public ulong LastPingSentTime { get; set; }

	/// <summary>
	/// Timestamp of the last pong received from this peer.
	/// </summary>
	public ulong LastPongReceivedTime { get; set; }
}