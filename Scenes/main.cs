using Godot;
using System;

public partial class main : Node
{
    [Export] private NodePath hostButtonPath;
    [Export] private NodePath joinButtonPath;
    [Export] private NodePath addressEntryPath;
    [Export] private NodePath mainMenuPath;
    [Export] private PackedScene characterScene;

    private Button hostButton;
    private Button joinButton;
    private LineEdit addressEntry;
    private Control mainMenu;

    private const int PORT = 9999;
    private bool isServerCreated = false;
    private bool isUpnpSetup = false;
    private ENetMultiplayerPeer enetPeer;

    public override void _Ready()
    {
        hostButton = GetNode<Button>(hostButtonPath);
        joinButton = GetNode<Button>(joinButtonPath);
        addressEntry = GetNode<LineEdit>(addressEntryPath);
        mainMenu = GetNode<Control>(mainMenuPath);

        hostButton.Pressed += OnHostButtonPressed;
        joinButton.Pressed += OnJoinButtonPressed;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
    }

    private void OnHostButtonPressed()
    {
        GD.Print("Host button pressed");

        if (isServerCreated)
        {
            GD.Print("Server already created");
            return;
        }

        mainMenu.Hide();
        enetPeer = new ENetMultiplayerPeer();
        Error error = enetPeer.CreateServer(PORT);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to create server: {error}");
            mainMenu.Show();
            return;
        }
        
        Multiplayer.MultiplayerPeer = enetPeer;
        isServerCreated = true;
        GD.Print($"Server created. Connect to: 127.0.0.1:{PORT}");
        AddPlayer(Multiplayer.GetUniqueId());
        Rpc(nameof(StartGame));
    }

    private void OnJoinButtonPressed()
    {
        mainMenu.Hide();
        enetPeer = new ENetMultiplayerPeer();
        var error = enetPeer.CreateClient("127.0.0.1", PORT);
        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to create client: {error}");
            return;
        }
        Multiplayer.MultiplayerPeer = enetPeer;
        GD.Print($"Connecting to {"127.0.0.1"}:{PORT}");
    }

    private void OnPeerConnected(long id)
    {
        GD.Print($"Peer connected: {id}");
        if (Multiplayer.IsServer())
        {
            AddPlayer(id);
        }
    }

    private void OnPeerDisconnected(long id)
    {
        GD.Print($"Peer disconnected: {id}");
        var player = GetNodeOrNull<Node>(id.ToString());
        if (player != null)
        {
            player.QueueFree();
        }
    }

    private void OnConnectedToServer()
    {
        GD.Print("Connected to server");
        AddPlayer(Multiplayer.GetUniqueId());
        StartGame();
    }

    private void OnConnectionFailed()
    {
        GD.PrintErr("Failed to connect to server");
        mainMenu.Show();
    }

    private void AddPlayer(long id)
    {
        var character = characterScene.Instantiate<CharacterMovement>();
        character.Name = id.ToString();
        character.SetMultiplayerAuthority((int)id);
        AddChild(character);
        GD.Print($"Added player: {id}");
    }

    private void StartGame()
    {
        // Load and instance your game world scene here
        var worldScene = ResourceLoader.Load<PackedScene>("res://Scenes/World.tscn").Instantiate<Node3D>();
        GetTree().Root.AddChild(worldScene);
    }
}