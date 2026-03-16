using System.Reflection.Metadata;
using Godot;

namespace SS3D;

public partial class Player : CharacterBody3D
{
	[ExportGroup("Node References")]
	[Export] public Camera3D? Camera { get; set; }

	[ExportGroup("Settings")]
	[Export] public float Speed { get; set; } = 5.0f;
	[Export] public float JumpVelocity { get; set; } = 4.5f;
	[Export] public float MouseSensitivity { get; set; } = 0.003f;
	[Export] public bool RdpCompatibility { get; set; } = false;
	public bool IsLocalPlayer { get; private set; }
	private float _cameraPitch;

	public override void _Ready()
	{
		IsLocalPlayer = Name == Multiplayer.GetUniqueId().ToString();

		if (IsLocalPlayer)
		{
			if (Camera is null)
			{
				GD.PrintErr("Player is missing a Camera3D reference. Assign Camera in the inspector or add a direct child named Camera3D.");
				return;
			}

			Camera.Current = true;

			Input.MouseMode = RdpCompatibility ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured;
			_cameraPitch = Camera.Rotation.X;
		}
	}

	public override void _ExitTree()
	{
		if (IsLocalPlayer)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsLocalPlayer)
		{
			var jumpInput = Input.IsActionJustPressed("ui_accept");
			var movementInput = Input.GetVector("strafe_left", "strafe_right", "move_forward", "move_backward");
			RpcId(1, nameof(HandleMovementInput), movementInput, jumpInput);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (IsLocalPlayer)
		{
			if (@event.IsActionPressed("toggle_mouse_capture"))
			{
				bool isLocked = Input.MouseMode is Input.MouseModeEnum.Captured or Input.MouseModeEnum.ConfinedHidden;
				Input.MouseMode = isLocked
					? Input.MouseModeEnum.Visible
					: (RdpCompatibility ? Input.MouseModeEnum.ConfinedHidden : Input.MouseModeEnum.Captured);
				GetViewport().SetInputAsHandled();
			}

			if (@event is not InputEventMouseMotion mouseMotionEvent || Input.MouseMode is not (Input.MouseModeEnum.Captured or Input.MouseModeEnum.ConfinedHidden))
			{
				return;
			}

			Vector2 mouseMotion = RdpCompatibility ? mouseMotionEvent.ScreenRelative : mouseMotionEvent.Relative;
			RpcId(1, nameof(HandleMouseMovement), mouseMotion);

			GetViewport().SetInputAsHandled();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void HandleMouseMovement(Vector2 mouseMotion)
	{
		RotateY(-mouseMotion.X * MouseSensitivity);

		_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Y * MouseSensitivity, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

		Vector3 cameraRotation = Camera.Rotation;
		cameraRotation.X = _cameraPitch;
		Camera.Rotation = cameraRotation;
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void HandleMovementInput(Vector2 movementInput, bool jumpPressed)
	{
		var velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)GetPhysicsProcessDeltaTime();
		}

		// Handle Jump.
		if (jumpPressed && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector3 direction = (Transform.Basis * new Vector3(movementInput.X, 0, movementInput.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
