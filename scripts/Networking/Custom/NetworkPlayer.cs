using Godot;

namespace KongleJam.Networking.Custom;

public class NetworkPlayer
{
    public string Username;
    public long Id;

    public NetworkPlayer(long id, string username)
    {
        Id = id;
        Username = username;
    }
}