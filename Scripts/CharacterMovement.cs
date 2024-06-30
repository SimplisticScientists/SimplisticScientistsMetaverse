using Godot;
using System;

public partial class CharacterMovement : CharacterBody3D
{
    [Export] public float Speed = 10.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.05f;
    [Export] public float Gravity = -9.8f;
    [Export] public float FireRate = 0.1f;
    private bool CanFire = true;
    private Camera3D _camera;
    private MultiplayerSynchronizer _synchronizer;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _synchronizer = GetNode<MultiplayerSynchronizer>("MultiplayerSynchronizer");

        // Ensure the synchronizer is set up correctly
        if (_synchronizer != null)
        {
            _synchronizer.SetMultiplayerAuthority(GetMultiplayerAuthority());
        }

        if (IsMultiplayerAuthority())
        {
            _camera.Current = true;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else
        {
            // Disable processing for non-authority players
            SetPhysicsProcess(false);
            SetProcessInput(false);
        }

        GD.Print($"Player {Name} initialized. Authority: {IsMultiplayerAuthority()}");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;

        // Handle Jump.
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
            velocity.Y = JumpVelocity;

        // Get the input direction and handle the movement/deceleration.
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
    public override void _Process(double delta)
    {
        if (IsMultiplayerAuthority() && Input.IsActionPressed("fire") && CanFire)
        {
            Fire();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;

        if (@event is InputEventMouseMotion mouseMotion)
        {
            // Rotate the player body horizontally
            RotateY(Mathf.DegToRad(-mouseMotion.Relative.X * MouseSensitivity));

            // Rotate the camera vertically
            _camera.RotateX(Mathf.DegToRad(-mouseMotion.Relative.Y * MouseSensitivity));
            _camera.Rotation = new Vector3(
                Mathf.Clamp(_camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)),
                _camera.Rotation.Y,
                _camera.Rotation.Z
            );
        }
    }

    // You can add additional methods here for player actions, such as shooting or interacting

    private async void Fire()
    {
        if (IsMultiplayerAuthority())
        {
            CanFire = false;
            Rpc(nameof(SyncFire));
            await ToSignal(GetTree().CreateTimer(FireRate), SceneTreeTimer.SignalName.Timeout);
            CanFire = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncFire(string action)
    {
        if (IsMultiplayerAuthority())
        {
            // Handle synced action here
            GD.Print($"Player {Name} fired their gun.");
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsMultiplayerAuthority())
        {
            // Handle damage logic here
            GD.Print($"Player {Name} took {amount} damage");
        }
    }

    // Example of a custom RPC method
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncAction(string action)
    {
        if (IsMultiplayerAuthority())
        {
            // Handle synced action here
            GD.Print($"Player {Name} performed action: {action}");
        }
    }
}