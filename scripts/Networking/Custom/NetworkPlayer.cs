using Godot;
using KongleJam.Resources;

namespace KongleJam.Networking.Custom;

public class NetworkPlayer
{
    public string Username;
    public long Id;
    public int Index;
    public int LocalIndex;

    public bool IsReady;
    public int CharacterId;

    public NetworkPlayer(long id, string username, int index, int localIndex)
    {
        Id = id;
        Username = username;
        Index = index;
        LocalIndex = localIndex;
        IsReady = false;
        CharacterId = -1;
    }
}