using Godot;
using System;

public partial class CharacterMovement : CharacterBody3D
{
    [Export] public float Speed = 10.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.05f;
    [Export] public float FireRate = 0.1f;

    private float Gravity => ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    private Camera3D Camera;
    private bool CanFire = true;
    private bool WasOnFloor;
    private bool Initialized;

    public override void _Ready()
    {
        Camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Initialize();
    }

    public void Initialize()
    {
        GD.Print("Initializing player state");
        GlobalTransform = new Transform3D(GlobalTransform.Basis, new Vector3(0, 10, 0));
        Velocity = Vector3.Zero;
        Initialized = true;
        GD.Print($"Player initialized at position: {GlobalTransform.Origin}");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Initialized)
            return;

            GD.Print($"W: {Input.IsActionPressed("move_forward")}, A: {Input.IsActionPressed("move_left")}, S: {Input.IsActionPressed("move_backward")}, D: {Input.IsActionPressed("move_right")}");
            GD.Print($"Jump: {Input.IsActionJustPressed("jump")}");

        var velocity = this.Velocity;
        var isOnFloor = IsOnFloor();

        if (isOnFloor != WasOnFloor)
        {
            GD.Print($"IsOnFloor changed: {isOnFloor}");
            WasOnFloor = isOnFloor;
        }

        if (!isOnFloor)
        {
            velocity.Y -= Gravity * (float)delta;
        }
        else
        {
            if (velocity.Y < 0)
            {
                velocity.Y = 0;
            }
        }

        // ... (other physics processing)

        this.Velocity = velocity;
        MoveAndSlide();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion MouseMotion)
        {
            RotateY(Mathf.DegToRad(-MouseMotion.Relative.X * MouseSensitivity));
            Camera.RotateX(Mathf.DegToRad(-MouseMotion.Relative.Y * MouseSensitivity));
            Camera.Rotation = new Vector3(
                Mathf.Clamp(Camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)),
                Camera.Rotation.Y,
                Camera.Rotation.Z);
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed("fire") && CanFire)
        {
            Fire();
        }
    }

    private async void Fire()
    {
        CanFire = false;
        GD.Print("Bang!"); // Replace with actual shooting logic later
        await ToSignal(GetTree().CreateTimer(FireRate), SceneTreeTimer.SignalName.Timeout);
        CanFire = true;
    }
}
