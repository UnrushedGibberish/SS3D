using Godot;
using System;

namespace SS3D;

public partial class MainMenu : Control
{
	[Export] public Button JoinButton { get; set; }
	[Export] public Button HostButton { get; set; }
	[Export] public Button QuitButton { get; set; }
	[Export] public LineEdit IPAddressInput { get; set; }
}
