using System.Collections.Generic;
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

        _createBtn.Visible = true;
        _joinBtn.Visible = true;
        _copyBtn.Visible = false;
        _leaveBtn.Visible = false;
    }

    public override void _Ready()
    {
    }

    private void HandleCreateLobby()
    {
        GD.Print("Trying to start server!");
        NetworkManager.Instance.StartServer(() => {
            CallDeferred(MethodName._UI_ServerStarted);
        }, () => {
            // GD.Print($"{NetworkManager.Instance.Players.Count}");
            foreach (KeyValuePair<int, NetworkPlayer> pair in NetworkManager.Instance.Players)
            {
                // GD.Print($"Player {pair.Key}: {pair.Value.Username} {pair.Value.Id}");
                GD.Print("aaaaaaa");
            }

            // CallDeferred(MethodName.RefreshPlayerList);
        });
    }

    private void RefreshPlayerList()
    {
        for (int i = _playerList.GetChildCount() - 1; i >= 0; ++i)
        {
            Node child = _playerList.GetChild(i);
            _playerList.RemoveChild(child);
            child.QueueFree();
        }

        CallDeferred(MethodName.RefreshPlayerListP2);
    }

    private void RefreshPlayerListP2()
    {
        // foreach (NetworkPlayer player in NetworkManager.Instance.Players.Values)
        // {
        //     Node disp = _playerDisplay.Instantiate();
        //     _playerList.AddChild(disp);
            
        //     // disp.GetNode<Label>("%Text").Text = player.Username;
        //     // disp.GetNode<TextureButton>("%Texture").ButtonDown += () => {
        //     //     NetworkManager.Instance.KickPlayer(player.Id);
        //     // };
        //     GD.Print($"Player: {player.Username}");
        //     GD.Print($"Player: {player.Id}");
        // }
    }

    private void _UI_ServerStarted()
    {
        _createBtn.Visible = false;
        _joinBtn.Visible = false;
        _copyBtn.Visible = true;
        _leaveBtn.Visible = true;
    }

    private void HandleJoinLobby()
    {
        string clipboard = DisplayServer.ClipboardGet();
        if (clipboard.Length == 0)
            return;

        GD.Print($"Trying to join lobby with IP: {clipboard}");

        NetworkManager.Instance.StartClient(clipboard, () => {
            CallDeferred(MethodName._UI_ClientStarted);
        });
    }

    private void _UI_ClientStarted()
    {
        _createBtn.Visible = false;
        _joinBtn.Visible = false;
        _copyBtn.Visible = true;
        _leaveBtn.Visible = true;
    }

    private void HandleCopyLobby()
    {
        DisplayServer.ClipboardSet(NetworkManager.Instance.GetLobbyCopy());
    }

    private void HandleLeaveLobby()
    {
        NetworkManager.Instance.Disconnect(() => {
            CallDeferred(MethodName._UI_ClientDisconnected);
        });
    }

    private void _UI_ClientDisconnected()
    {
        _createBtn.Visible = true;
        _joinBtn.Visible = true;
        _copyBtn.Visible = false;
        _leaveBtn.Visible = false;
    }
}