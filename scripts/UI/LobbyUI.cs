using Godot;

namespace KongleJam.Managers;

public partial class LobbyUI : Node 
{
    private TextureButton _createBtn;
    private TextureButton _joinBtn;
    private TextureButton _copyBtn;
    private TextureButton _leaveBtn;

    public override void _EnterTree()
    {
        _createBtn = GetNode<TextureButton>("%CreateLobby");
        _joinBtn = GetNode<TextureButton>("%JoinClipboard");
        _copyBtn = GetNode<TextureButton>("%CopyCode");
        _leaveBtn = GetNode<TextureButton>("%LeaveLobby");

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

    private void HandleCreateLobby()
    {
        GD.Print("Trying to start server!");
        NetworkManager.Instance.StartServer(() => {
            CallDeferred(MethodName._UI_ServerStarted);
        });
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