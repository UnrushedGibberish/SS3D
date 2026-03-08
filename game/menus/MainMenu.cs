using Godot;
using System;

namespace SS3D;

public partial class MainMenu : Control
{
	[ExportGroup("Node References")]
	[Export] public Button? JoinButton { get; set; }
	[Export] public Button? HostButton { get; set; }
	[Export] public Button? QuitButton { get; set; }
	[Export] public LineEdit? IPAddressInput { get; set; }
	[Export] public Label? StatusLabel { get; set; }

	private const ushort DefaultPort = 7777;

	public override void _Ready()
	{
		if (JoinButton == null || HostButton == null || QuitButton == null || IPAddressInput == null || StatusLabel == null)
		{
			GD.PrintErr("Node references are missing.");
			return;
		}

		JoinButton.Pressed += OnJoinPressed;
		HostButton.Pressed += OnHostPressed;
		QuitButton.Pressed += OnQuitPressed;
	}

	public void SetStatus(string text)
	{
		if (StatusLabel == null)
		{
			GD.PrintErr("Status label not found.");
			return;
		}
		
		StatusLabel.Text = text;
	}

	private void OnJoinPressed()
	{
		if (NetworkManager.Instance == null)
		{
			SetStatus("Network manager not found.");
			GD.PrintErr("Network manager not found.");
			return;
		}

		if (NetworkManager.Instance.IsConnected)
		{
			SetStatus("Already connected.");
			return;
		}

		var hostText = IPAddressInput?.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(hostText))
		{
			SetStatus("Enter host address.");
			return;
		}

		// Allow "localhost" or an IP address; basic validation
		var isLocal = string.Equals(hostText, "localhost", StringComparison.OrdinalIgnoreCase);
		if (!isLocal)
		{
			if (!System.Net.IPAddress.TryParse(hostText, out var _))
			{
				SetStatus("Invalid IP address.");
				return;
			}
		}

		var err = NetworkManager.Instance.Join(hostText, DefaultPort);
		if (err != Error.Ok)
		{
			SetStatus($"Join failed: {err}");
			return;
		}

		SetStatus("Connecting...");
	}

	private void OnHostPressed()
	{
		if (NetworkManager.Instance == null)
		{
			SetStatus("Network manager not found.");
			GD.PrintErr("Network manager not found.");
			return;
		}

		if (NetworkManager.Instance.IsConnected)
		{
			SetStatus("Already connected or hosting.");
			return;
		}

		var err = NetworkManager.Instance.Host(DefaultPort);
		if (err != Error.Ok)
		{
			SetStatus($"Host failed: {err}");
			return;
		}

		SetStatus($"Hosting on port {DefaultPort}...");
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
