using Godot;
using System;

public partial class CharacterMovement : CharacterBody3D
{
    [Export]
    public float Speed = 10.0f;

    [Export]
    public float JumpVelocity = 4.5f;

    [Export]
    public float MouseSensitivity = 0.05f;

    [Export]
    public float FireRate = 0.1f;

    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private Camera3D _camera;
    private bool _canFire = true;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta;

        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;

        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
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

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
            _camera.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
            _camera.Rotation = new Vector3(
                Mathf.Clamp(_camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)),
                _camera.Rotation.Y,
                _camera.Rotation.Z);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("fire") && _canFire)
        {
            Fire();
        }
    }

    private async void Fire()
    {
        _canFire = false;
        GD.Print("Bang!"); // Replace with actual shooting logic later
        await ToSignal(GetTree().CreateTimer(FireRate), SceneTreeTimer.SignalName.Timeout);
        _canFire = true;
    }
}
