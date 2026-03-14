using Godot;
using System;

namespace SS3D;

public partial class NetworkedCube : Node3D
{
	public override void _PhysicsProcess(double delta)
	{
		if (!Multiplayer.IsServer())
		{
			return;
		}
		
		RotateY((float)delta);
	}

}
