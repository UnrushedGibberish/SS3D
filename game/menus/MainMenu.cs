using Godot;
using SS3D.Networking;
using System;

namespace SS3D;

public partial class MainMenu : Control
{
	[ExportGroup("Node References")]
	[Export] public Button? JoinButton { get; set; }
	[Export] public Button? HostButton { get; set; }
	[Export] public Button? QuitButton { get; set; }
	[Export] public LineEdit? IPAddressInput { get; set; }

	private const ushort DefaultPort = 7777;

	public override void _Ready()
	{
		if (JoinButton == null || HostButton == null || QuitButton == null || IPAddressInput == null)
		{
			GD.PrintErr("Node references are missing.");
			return;
		}

		HostButton.Pressed += OnHostPressed;
		JoinButton.Pressed += OnJoinPressed;
		QuitButton.Pressed += OnQuitPressed;
	}

	private void OnHostPressed()
	{
		if (NetworkManager.Instance == null)
		{
			GD.PrintErr("Network manager not found.");
			return;
		}

		if (NetworkManager.Instance.HasConnection)
		{
			GD.PrintErr("Already connected or hosting.");
			return;
		}

		var err = NetworkManager.Instance.CreateHost(DefaultPort);
		if (err != Error.Ok)
		{
			GD.PrintErr($"Host failed: {err}");
			return;
		}

		GD.Print($"Hosting on port {DefaultPort}...");
	}

	private void OnJoinPressed()
	{
		if (NetworkManager.Instance == null)
		{
			GD.PrintErr("Network manager not found.");
			return;
		}

		if (NetworkManager.Instance.HasConnection)
		{
			GD.PrintErr("Already connected.");
			return;
		}

		var hostText = IPAddressInput?.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(hostText))
		{
			GD.PrintErr("Enter host address.");
			return;
		}

		// Allow "localhost" or an IP address; basic validation
		var isLocal = string.Equals(hostText, "localhost", StringComparison.OrdinalIgnoreCase);
		if (!isLocal)
		{
			if (!System.Net.IPAddress.TryParse(hostText, out var _))
			{
				GD.PrintErr("Invalid IP address.");
				return;
			}
		}

		var err = NetworkManager.Instance.CreateClient(hostText, DefaultPort);
		if (err != Error.Ok)
		{
			GD.PrintErr($"Join failed: {err}");
			return;
		}

		GD.Print("Connecting...");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
