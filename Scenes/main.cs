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

        Multiplayer.PeerConnected += PeerConnected;
        Multiplayer.PeerDisconnected += PeerDisconnected;
        Multiplayer.ConnectedToServer += ConnectedToServer;
        Multiplayer.ConnectionFailed += ConnectionFailed;
    }

    private void ConnectionFailed()
    {
        GD.Print("Connection failed.");
    }

    private void ConnectedToServer()
    {
        GD.Print("Connected to server.");
    }

    private void PeerDisconnected(long id)
    {
        GD.Print("Player disconnected!" + id.ToString());
    }

    private void PeerConnected(long id)
    {
        GD.Print("Player connected!" + id.ToString());
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
            GD.PrintErr($"Failed to create server. Error: {error}");
            mainMenu.Show();
            return;
        }

        Multiplayer.MultiplayerPeer = enetPeer;
        isServerCreated = true;
        GD.Print($"Server created. Connect to: 127.0.0.1:{PORT}");

        if (!isPeerConnectedHandlerConnected)
        {
            Multiplayer.PeerConnected += (long id) => AddPlayer(id);
            isPeerConnectedHandlerConnected = true;
        }

        if (!isPeerDisconnectedHandlerConnected)
        {
            Multiplayer.PeerDisconnected += (long id) => RemovePlayer(id);
            isPeerDisconnectedHandlerConnected = true;
        }

        AddPlayer((long)Multiplayer.GetUniqueId());
        Rpc("StartGame");
    }

    private async void OnJoinButtonPressed()
    {
        GD.Print("Join button pressed");

        //By removing the connection status check, you're allowing the game to proceed with the connection attempt regardless of the current connection status. This can be useful in scenarios where you want to allow multiple instances of the game to connect to the server, but it's important to ensure that the server and networking setup can handle multiple connections correctly.

        mainMenu.Hide();

        enetPeer = new ENetMultiplayerPeer();
        GD.Print($"Attempting to connect to 127.0.0.1:{PORT}");
        Error error = enetPeer.CreateClient("127.0.0.1", PORT);

        if (error != Error.Ok)
        {
            GD.PrintErr($"Failed to create client. Error: {error}");
            mainMenu.Show();
            return;
        }

        GD.Print("Client created, setting MultiplayerPeer");
        Multiplayer.MultiplayerPeer = enetPeer;
        Rpc("StartGame");
    }

    private void AddPlayer(long peerId)
    {
        if (!Multiplayer.IsServer())
            return;

        try
        {
            var player = playerScene.Instantiate<CharacterMovement>();
            if (player != null)
            {
                GD.Print($"Instantiating player for peerId {peerId}");
                player.Name = peerId.ToString();
                player.SetMultiplayerAuthority((int)peerId);  // Cast long to int here
                AddChild(player);
                player.GlobalTransform = new Transform3D(player.GlobalTransform.Basis, new Vector3(0, 10, 0));
                GD.Print($"Player {peerId} initial position: {player.GlobalTransform.Origin}");
                player.Initialize();
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void StartGame()
    {
        var scene = ResourceLoader.Load<PackedScene>("res://main.tscn").Instantiate<Node3D>();
        GetTree().Root.AddChild(scene);
    }



    private void RemovePlayer(long peerId)
    {
        var player = GetNodeOrNull<Node>(peerId.ToString());
        player?.QueueFree();
    }

    private void UpnpSetup()
    {
        var upnp = new Upnp();

        var discoverResult = upnp.Discover();
        if (discoverResult != 0)
        {
            GD.PrintErr($"UPNP Discover Failed! Error code: {discoverResult}");
            return;
        }

        if (upnp.GetGateway() == null || !upnp.GetGateway().IsValidGateway())
        {
            GD.PrintErr("UPNP No Valid Gateway Found!");
            return;
        }

        var mapResult = upnp.AddPortMapping(PORT);
        if (mapResult != 0)
        {
            GD.PrintErr($"UPNP Port Mapping Failed! Error code: {mapResult}");
            return;
        }

        var externalIp = upnp.QueryExternalAddress();
        GD.Print($"Success! Join Address: {externalIp}");
    }
}