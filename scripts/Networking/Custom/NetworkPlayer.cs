using Godot;

namespace KongleJam.Networking.Custom;

public partial class NetworkPlayer : GodotObject 
{
    public string Username;
    public long Id;

    public NetworkPlayer(long id, string username)
    {
        Id = id;
        Username = username;
    }
}