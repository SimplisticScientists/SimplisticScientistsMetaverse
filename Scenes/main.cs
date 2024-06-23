using Godot;
using System;
using Godot.NativeInterop;

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
    // private Control hud;
    // private ProgressBar healthBar;

    private PackedScene playerScene;
    private const int PORT = 9999;
    private ENetMultiplayerPeer enetPeer;

    public override void _Ready()
    {
        hostButton = GetNode<Button>(hostButtonPath);
        joinButton = GetNode<Button>(joinButtonPath);
        addressEntry = GetNode<LineEdit>(addressEntryPath);
        mainMenu = GetNode<Control>(mainMenuPath);
        // hud = GetNode<Control>(hudPath);
        // healthBar = GetNode<ProgressBar>(healthBarPath);

        playerScene = (PackedScene)ResourceLoader.Load("res://Scenes/character.tscn");
        enetPeer = new ENetMultiplayerPeer();

        // Connect button signals
        hostButton.Pressed += OnHostButtonPressed;
        joinButton.Pressed += OnJoinButtonPressed;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("quit"))
        {
            GetTree().Quit();
        }
    }

    private void OnHostButtonPressed()
    {
        GD.Print("Host button pressed");
        mainMenu.Hide();
        // hud.Show();

        enetPeer.CreateServer(PORT);
        Multiplayer.MultiplayerPeer = enetPeer;
        Multiplayer.PeerConnected += AddPlayer;
        Multiplayer.PeerDisconnected += RemovePlayer;

        AddPlayer(Multiplayer.GetUniqueId());

        UpnpSetup();
    }

    private void OnJoinButtonPressed()
    {
        GD.Print("Join button pressed");
        mainMenu.Hide();
        // hud.Show();

        // Swap this back when trying to connect not locally
        // enetPeer.CreateClient(addressEntry.Text, PORT);
        enetPeer.CreateClient("localhost", PORT);

        Multiplayer.MultiplayerPeer = enetPeer;
    }

    private void AddPlayer(long peerId)
    {
        try
        {
           var player = playerScene.Instantiate<CharacterBody3D>(); // This will create an instance of the scene
        if (player != null)
        {
            AddChild(player); // Add the instantiated scene as a child of the Main node
        }
        else
        {
            GD.PrintErr("Failed to instance player scene.");
        }
            // player.Name = peerId.ToString();
            /// AddChild(player);

            // The line below is required to make the node visible in the Scene tree dock
            // and persist changes made by the tool script to the saved scene file.
            // player.Owner = GetTree().EditedSceneRoot;

            // Example: Set the player's initial position to a specific vector
            // var spawnPosition = new Vector3(66, 25, 0); // Adjust X, Y, Z as needed
            // player.GlobalTransform = new Transform3D(player.GlobalTransform.Basis, spawnPosition);

            // GD.Print($"Player {peerId} spawned at position: {player.GlobalTransform.Origin}");

            //if (player.IsMultiplayerAuthority())
            //{
                // Assuming Player script has a health_changed signal
                // This is commented out as we are not handling health bar updates now
                // player.Connect("health_changed", new Callable(this, nameof(UpdateHealthBar)));
            //}
        }
        catch (Exception e)
        {
            GD.PrintErr("Exception in AddPlayer: ", e.ToString());
        }
    }

    private void RemovePlayer(long peerId)
    {
        var player = GetNodeOrNull<Node>(peerId.ToString());
        player?.QueueFree();
    }

    //private void UpdateHealthBar(float healthValue)
    //{
        //healthBar.Value = healthValue;
    //}

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
