using Godot;
using System;

public partial class CharacterMovement : CharacterBody3D
{
    [Export] public float Speed = 10.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.05f;
    [Export] public float FireRate = 0.1f;

    private float Gravity => (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
    private Camera3D Camera;
    private bool CanFire = true;
    private bool Initialized;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(Name.GetHashCode());
    }
    public override void _Ready()
    {
        
        Camera = GetNode<Camera3D>("Camera3D");
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Camera.Current = true;
        Initialize();
    }

    public void Initialize()
    {
        GD.Print("Initializing player state");
        GlobalPosition = new Vector3(0, 10, 0);
        Velocity = Vector3.Zero;
        Initialized = true;
        GD.Print($"Player initialized at position: {GlobalPosition}");
    }

    public override void _PhysicsProcess(double delta)
    {
       
        Vector3 velocity = Velocity;

        if (!IsOnFloor())
            velocity.Y -= Gravity * (float)delta;

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
            velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        Rpc(nameof(SyncPosition), GlobalPosition);
    }

    public override void _Input(InputEvent @event)
    {

        if (@event is InputEventMouseMotion mouseMotion)
        {
            RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));
            Camera.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
            Camera.Rotation = new Vector3(
                Mathf.Clamp(Camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)),
                Camera.Rotation.Y,
                Camera.Rotation.Z);

            Rpc(nameof(SyncRotation), Rotation, Camera.Rotation);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
            return;

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

    [Rpc]
    private void SyncPosition(Vector3 position)
    {
        GlobalPosition = position;
    }

    [Rpc]
    private void SyncRotation(Vector3 bodyRotation, Vector3 cameraRotation)
    {
        Rotation = bodyRotation;
        if (Camera != null)
        {
            Camera.Rotation = cameraRotation;
        }
    }
}