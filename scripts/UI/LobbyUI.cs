using Godot;
using KongleJam.Networking.Custom;

namespace KongleJam.Managers;

public partial class LobbyUI : Node 
{
    [ExportGroup("Scenes")]  
    [Export] private PackedScene _playerDisplay;
   
    private TextureButton _createBtn;
    private TextureButton _joinBtn;
    private TextureButton _copyBtn;
    private TextureButton _leaveBtn;

    private VBoxContainer _playerList;

    public override void _EnterTree()
    {
        _createBtn = GetNode<TextureButton>("%CreateLobby");
        _joinBtn = GetNode<TextureButton>("%JoinClipboard");
        _copyBtn = GetNode<TextureButton>("%CopyCode");
        _leaveBtn = GetNode<TextureButton>("%LeaveLobby");

        _playerList = GetNode<VBoxContainer>("%PlayerList");

        _createBtn.Connect(
            TextureButton.SignalName.ButtonDown, 
            new Callable(this, MethodName.HandleCreateLobby)
        );
        _joinBtn.Connect(
            TextureButton.SignalName.ButtonDown, 
            new Callable(this, MethodName.HandleJoinLobby)
        );
        _copyBtn.Connect(
            TextureButton.SignalName.ButtonDown, 
            new Callable(this, MethodName.HandleCopyLobby)
        );
        _leaveBtn.Connect(
            TextureButton.SignalName.ButtonDown, 
            new Callable(this, MethodName.HandleLeaveLobby)
        );

        UI_Disconnected();
    }

    public override void _Ready()
    {
        NetworkManager.Instance.OnServerStarted += () => {
            UI_ServerStarted();
        };
        NetworkManager.Instance.OnDisconnected += () => {
            UI_Disconnected();
        };
        NetworkManager.Instance.OnClientConnectedToServer += () => {
            UI_ClientConnected();
        };
        NetworkManager.Instance.OnBroadcastPlayers += (players) => {
            UI_RefreshPlayerList(players);
        };
    }

    // BUTTON HANDLERS
    private void HandleCreateLobby()
    {
        GD.Print("LOBBY_UI: Trying to start server!");
        NetworkManager.Instance.HostServer();
    }

    private void HandleJoinLobby()
    {
        string clipboard = DisplayServer.ClipboardGet();
        if (clipboard.Length == 0)
            return;

        GD.Print($"LOBBY_UI: Trying to join lobby with IP: {clipboard}");
        NetworkManager.Instance.JoinServer(clipboard);
    }

    private void HandleCopyLobby()
    {
        // DisplayServer.ClipboardSet(NetworkManager.Instance.GetLobbyCopy());
    }

    private void HandleLeaveLobby()
    {
    }

    // UI STUFF
    private void UI_ServerStarted()
    {
        _createBtn.Visible = false;
        _joinBtn.Visible = false;
        _copyBtn.Visible = true;
        _leaveBtn.Visible = true;
    }

    private void UI_ClientConnected()
    {
        _createBtn.Visible = false;
        _joinBtn.Visible = false;
        _copyBtn.Visible = true;
        _leaveBtn.Visible = true;
    }
    
    private void UI_Disconnected()
    {
        _createBtn.Visible = true;
        _joinBtn.Visible = true;
        _copyBtn.Visible = false;
        _leaveBtn.Visible = false;
    }

    private void UI_RefreshPlayerList(NetworkPlayer[] players)
    {
        for (int i = _playerList.GetChildCount() - 1; i >= 0; ++i)
        {
            Node child = _playerList.GetChild(i);
            _playerList.RemoveChild(child);
            child.QueueFree();
        }

        foreach (NetworkPlayer player in players)
        {
            Node disp = _playerDisplay.Instantiate();
            disp.GetNode<Label>("%Text").Text = player.Username;
            if (NetworkManager.IsServer && !NetworkManager.IsSelfId(player.Id))
            {
                disp.GetNode<TextureButton>("%Texture").ButtonDown += () => {
                    NetworkManager.Instance.KickPlayer(NetworkManager.PeerId);
                };
            }

            _playerList.AddChild(disp);
        }
    }
}