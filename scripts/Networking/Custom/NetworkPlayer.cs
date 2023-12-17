using Godot;
using KongleJam.Resources;

namespace KongleJam.Networking.Custom;

public class NetworkPlayer
{
    public string Username;
    public long Id;
    public int Index;

    public bool IsReady;
    public int CharacterId;

    public NetworkPlayer(long id, string username, int index)
    {
        Id = id;
        Username = username;
        Index = index;
        IsReady = false;
        CharacterId = -1;
    }
}