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

	private float _cameraPitch;

	public override void _Ready()
	{
		if (Camera is null)
		{
			GD.PrintErr("Player is missing a Camera3D reference. Assign Camera in the inspector or add a direct child named Camera3D.");
			return;
		}

		Input.MouseMode = Input.MouseModeEnum.Captured;
		_cameraPitch = Camera.Rotation.X;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!NetworkManager.Instance.IsServer)
		{
			return;
		}

		var velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}
		
		Vector2 inputDir = Input.GetVector("strafe_left", "strafe_right", "move_forward", "move_backward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
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

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("toggle_mouse_capture"))
		{
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
			GetViewport().SetInputAsHandled();
		}

		if (@event is not InputEventMouseMotion mouseMotion || Input.MouseMode != Input.MouseModeEnum.Captured)
		{
			return;
		}

		if (Camera is null)
		{
			return;
		}

		RotateY(-mouseMotion.Relative.X * MouseSensitivity);

		_cameraPitch = Mathf.Clamp(_cameraPitch - mouseMotion.Relative.Y * MouseSensitivity, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

		Vector3 cameraRotation = Camera.Rotation;
		cameraRotation.X = _cameraPitch;
		Camera.Rotation = cameraRotation;

		GetViewport().SetInputAsHandled();
	}
}
