using Godot;
using System;

public partial class main : Node
{
    [Export] private NodePath hostButtonPath;
    [Export] private NodePath joinButtonPath;
    [Export] private NodePath addressEntryPath;
    [Export] private NodePath mainMenuPath;

    private Button hostButton;
    private Button joinButton;
    private LineEdit addressEntry;
    private Control mainMenu;
    private PackedScene playerScene;
    private const int PORT = 9999;
    private ENetMultiplayerPeer enetPeer;

    private bool isServerCreated = false;
    private bool isUpnpSetup = false;

    private bool isPeerConnectedHandlerConnected = false;
    private bool isPeerDisconnectedHandlerConnected = false;
    private bool isPlayerInitialized = false;

    public override void _Ready()
    {
        hostButton = GetNode<Button>(hostButtonPath);
        joinButton = GetNode<Button>(joinButtonPath);
        addressEntry = GetNode<LineEdit>(addressEntryPath);
        mainMenu = GetNode<Control>(mainMenuPath);
        playerScene = (PackedScene)ResourceLoader.Load("res://Scenes/character.tscn");
        enetPeer = new ENetMultiplayerPeer();

        hostButton.Pressed += OnHostButtonPressed;
        joinButton.Pressed += OnJoinButtonPressed;
    }

    private void OnHostButtonPressed()
    {
        GD.Print("Host button pressed");
        mainMenu.Hide();

        if (!isServerCreated)
        {
            enetPeer.CreateServer(PORT);
            Multiplayer.MultiplayerPeer = enetPeer;
            isServerCreated = true;
        }

        if (!isUpnpSetup)
        {
            UpnpSetup();
            isUpnpSetup = true;
        }

        // Connect the event handlers if they are not already connected
        if (!isPeerConnectedHandlerConnected)
        {
            Multiplayer.PeerConnected += AddPlayer;
            isPeerConnectedHandlerConnected = true;
        }

        if (!isPeerDisconnectedHandlerConnected)
        {
            Multiplayer.PeerDisconnected += RemovePlayer;
            isPeerDisconnectedHandlerConnected = true;
        }

        // Initialize the player only once
        if (!isPlayerInitialized)
        {
            AddPlayer(Multiplayer.GetUniqueId());
            isPlayerInitialized = true;
        }
    }

    private void OnJoinButtonPressed()
    {
        GD.Print("Join button pressed");
        mainMenu.Hide();
        enetPeer.CreateClient("localhost", PORT);
        Multiplayer.MultiplayerPeer = enetPeer;
    }

    private void AddPlayer(long PeerId)
    {
        try
        {
            var Player = playerScene.Instantiate<CharacterMovement>();
            if (Player != null)
            {
                GD.Print($"Instantiating player for peerId {PeerId}");
                AddChild(Player);
                Player.GlobalTransform = new Transform3D(Player.GlobalTransform.Basis, new Vector3(0, 10, 0));
                GD.Print($"Player {PeerId} initial position: {Player.GlobalTransform.Origin}");
                Player.Initialize();
            }
            else
            {
                GD.PrintErr("Failed to instance player scene.");
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Exception in AddPlayer: ", e.ToString());
        }
    }

    private void RemovePlayer(long PeerId)
    {
        var Player = GetNodeOrNull<Node>(PeerId.ToString());
        Player?.QueueFree();
    }

    private void UpnpSetup()
    {
        var upnp = new Upnp();

        var discoverResult = upnp.Discover();
        if (discoverResult != 0)  // 0 typically means success
        {
            GD.PrintErr($"UPNP Discover Failed! Error code: {discoverResult}");
            return;
        }

        // Check if we have a valid gateway
        if (upnp.GetGateway() == null || !upnp.GetGateway().IsValidGateway())
        {
            GD.PrintErr("UPNP No Valid Gateway Found!");
            return;
        }

        var mapResult = upnp.AddPortMapping(PORT);
        if (mapResult != 0)  // 0 typically means success
        {
            GD.PrintErr($"UPNP Port Mapping Failed! Error code: {mapResult}");
            return;
        }

        var externalIp = upnp.QueryExternalAddress();
        GD.Print($"Success! Join Address: {externalIp}");
    }
}