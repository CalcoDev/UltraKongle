using Godot;
using KongleJam.Networking.Custom;
using KongleJam.Managers;

namespace KongleJam.Ui;

public partial class LobbyUI : Node 
{
    [ExportGroup("Scenes")]  
    [Export] private PackedScene _playerDisplay;
   
    private TextureButton _createBtn;
    private TextureButton _joinBtn;
    private TextureButton _copyBtn;
    private TextureButton _leaveBtn;
    private TextureButton _startBtn;

    private VBoxContainer _playerList;
    private TextureRect _playerListContainer;

    private LineEdit _usernameInput;
    private TextureButton _hidePlayersBtn;

    public override void _EnterTree()
    {
        _createBtn = GetNode<TextureButton>("%CreateLobby");
        _joinBtn = GetNode<TextureButton>("%JoinClipboard");
        _copyBtn = GetNode<TextureButton>("%CopyCode");
        _leaveBtn = GetNode<TextureButton>("%LeaveLobby");
        _startBtn = GetNode<TextureButton>("%StartGame");
        _usernameInput = GetNode<LineEdit>("%UsernameInput");
        _hidePlayersBtn = GetNode<TextureButton>("%HidePlayers");
        _playerListContainer = GetNode<TextureRect>("%Players");

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
        _startBtn.Connect(
            TextureButton.SignalName.ButtonDown, 
            new Callable(this, MethodName.HandleStartGame)
        );

        _hidePlayersBtn.Connect(
            TextureButton.SignalName.ButtonDown, 
            new Callable(this, MethodName.HandleHidePlayers)
        );

        UI_Disconnected();
    }

    public override void _Ready()
    {
        NetworkManager.Instance.OnServerStarted += HandleServerStarted;
        NetworkManager.Instance.OnDisconnected += HandleDisconnected;
        NetworkManager.Instance.OnClientConnectedToServer += HandleClientConnected;
        NetworkManager.Instance.OnServerConnectionsChanged += HandleServerConnectionsChanged;
    }

    public override void _ExitTree()
    {
        NetworkManager.Instance.OnServerStarted -= HandleServerStarted;
        NetworkManager.Instance.OnDisconnected -= HandleDisconnected;
        NetworkManager.Instance.OnClientConnectedToServer -= HandleClientConnected;
        NetworkManager.Instance.OnServerConnectionsChanged -= HandleServerConnectionsChanged;   
    }

    // NETWORK MANAGER HANDLERS
    private void HandleServerStarted()
    {
        UI_ServerStarted();
    }

    private void HandleDisconnected()
    {
        UI_Disconnected();
    }

    private void HandleClientConnected()
    {
        UI_ClientConnected();
    }

    private void HandleServerConnectionsChanged()
    {
        CallDeferred(MethodName.UI_RefreshPlayerList);
    }

    // BUTTON HANDLERS
    private void HandleCreateLobby()
    {
        NetworkManager.Instance.HostServer(_usernameInput.Text);
        _usernameInput.Text = "";
    }

    private void HandleJoinLobby()
    {
        string clipboard = DisplayServer.ClipboardGet();
        if (clipboard.Length == 0)
            return;

        NetworkManager.Instance.JoinServer(clipboard, _usernameInput.Text);
        _usernameInput.Text = "";
    }

    private void HandleCopyLobby()
    {
        DisplayServer.ClipboardSet(NetworkManager.GetLobbyCode());
    }

    private void HandleLeaveLobby()
    {
        NetworkManager.Instance.Disconnect();
    }

    private void HandleStartGame()
    {
        NetworkManager.Instance.GoToCharacterSelect();
    }

    private void HandleHidePlayers()
    {
        _playerListContainer.Visible = !_playerListContainer.Visible;
        
        if (_playerListContainer.Visible)
            _hidePlayersBtn.Position = new Vector2(158, 233);
        else
            _hidePlayersBtn.Position = new Vector2(8, 233);
    }

    // UI STUFF
    private void UI_ServerStarted()
    {
        _createBtn.Visible = false;
        _joinBtn.Visible = false;
        _copyBtn.Visible = true;
        _leaveBtn.Visible = true;
        _usernameInput.Visible = false;

        _startBtn.Visible = true; 
    }

    private void UI_ClientConnected()
    {
        _createBtn.Visible = false;
        _joinBtn.Visible = false;
        _copyBtn.Visible = true;
        _leaveBtn.Visible = true;
        _usernameInput.Visible = false;
        
        _startBtn.Visible = false; 
    }
    
    private void UI_Disconnected()
    {
        _createBtn.Visible = true;
        _joinBtn.Visible = true;
        _copyBtn.Visible = false;
        _leaveBtn.Visible = false;
        _usernameInput.Visible = true;
        
        _startBtn.Visible = false;
    }

    private void UI_RefreshPlayerList()
    {
        foreach (Node child in _playerList.GetChildren())
        {
            _playerList.RemoveChild(child);
            child.Free();
        }

        foreach (NetworkPlayer player in NetworkManager.Instance.Players.Values)
        {
            Node disp = _playerDisplay.Instantiate();
            Label lbl = disp.GetNode<Label>("%Text");
            
            lbl.Text = "";
            if (NetworkManager.IsSelfId(player.Id))
                lbl.Text += "S";
            if (NetworkManager.ServerPeerId == player.Id)
                lbl.Text += "H";
            if (lbl.Text.Length != 0)
                lbl.Text += "-";
            lbl.Text += player.Username;

            if (NetworkManager.IsServer && !NetworkManager.IsSelfId(player.Id))
            {
                disp.GetNode<TextureButton>("%Texture").ButtonDown += () => {
                    NetworkManager.Instance.KickPlayer(player.Id);
                };
            }
            else
            {
                disp.GetNode<TextureButton>("%Texture").Visible = false;
            }

            _playerList.AddChild(disp);
        }
    }
}