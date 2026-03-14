using Godot;
using System;

namespace SS3D;

public partial class Map : Node3D
{
	[ExportGroup("Spawn Points")]
	[Export] public Node3D[] PlayerSpawnPoints { get; set; } = Array.Empty<Node3D>();

	[ExportGroup("Scene References")]
	[Export] public PackedScene NetworkedCubeScene { get; set; }

	private int _nextSpawnIndex = 0;

	public override void _Ready()
	{
		if (NetworkedCubeScene == null)
		{
			GD.PrintErr("NetworkedCubeScene is not assigned in the Map.");
		}

		if (Multiplayer.IsServer())
		{
			for (int i = 0; i < 5; i++)
			{
				var cubeInstance = NetworkedCubeScene.Instantiate<NetworkedCube>();
				AddChild(cubeInstance);
				cubeInstance.GlobalPosition = new Vector3(i * 2, 1, 0);
			}
		}
	}

	public Vector3 GetNextSpawnPosition()
	{
		if (PlayerSpawnPoints.Length == 0)
		{
			GD.PrintErr("No player spawn points defined in the map.");
			return Vector3.Zero;
		}

		var spawnPoint = PlayerSpawnPoints[_nextSpawnIndex];
		_nextSpawnIndex = (_nextSpawnIndex + 1) % PlayerSpawnPoints.Length;
		return spawnPoint.GlobalPosition;
	}
}
