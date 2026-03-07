using Godot;
using System;

namespace SS3D;

public partial class Main : Node
{
	[Export] public Node3D WorldRoot { get; set; }
	[Export] public CanvasLayer GUIRoot { get; set; }
	[Export] public PackedScene MainMenuScene { get; set; }
}
