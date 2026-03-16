using Godot;
using System;

namespace SS3D;

public partial class Map : Node3D
{
	[ExportGroup("Node References")]
	[Export] public MultiplayerSpawner? Spawner { get; set; }

	[ExportGroup("Scene References")]
	[Export] public PackedScene? NetworkedCubeScene { get; set; }

	public override void _Ready()
	{
		if (Spawner == null)
		{
			GD.PrintErr("Node references are missing");
			return;
		}

		if (NetworkedCubeScene == null)
		{
			GD.PrintErr("NetworkedCubeScene is not assigned in the Map.");
			return;
		}

		Spawner.Spawned += OnSpawn;

		if (Multiplayer.IsServer())
		{
			for (int i = 0; i < 5; i++)
			{
				var cubeInstance = NetworkedCubeScene?.Instantiate<NetworkedCube>();
				if (cubeInstance != null)
				{
					AddChild(node: cubeInstance, forceReadableName: true);
					cubeInstance.GlobalPosition = new Vector3(i * 2, 1, 0);
				}
				else
				{
					GD.PrintErr("Failed to instantiate NetworkedCubeScene.");
				}
			}
		}
	}

	private void OnSpawn(Node node)
	{
		GD.Print($"Node spawned: {node.Name}");
	}
}
