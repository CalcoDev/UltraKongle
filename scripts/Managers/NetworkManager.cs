using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KongleJam.Networking.Custom;

namespace KongleJam.Managers;

public partial class NetworkManager : Node
{
    public const int ServerPort = 25565;
    public static int ClientPort { get; private set; } = 0;

    public static NetworkManager Instance { get; private set; }

    public static bool IsServer { get; private set; } = false;
    public static long Id { get; private set; } = -1;
    public static int PeerId { get; private set; } = -1;
    
    // Store all players
    public Dictionary<long, NetworkPlayer> Players;

    // Called ON CLIENT AND SERVER
    public Action OnDisconnected;

    // Called ON THE CLIENT when CLIENT connects to SERVER
    public Action OnClientConnectedToServer;

    // Called ON THE SERVER when SERVER starts
    public Action OnServerStarted;

    // Called ON THE SERVER when CLIENT connects to SERVER
    public Action OnServerConnectionsChanged;

    // Called via RPC
    public Action<NetworkPlayer[]> OnBroadcastPlayers;
    
    private ENetMultiplayerPeer _peer;

    private bool _isInit = false;

    // STATIC HELPERS 
    public static bool IsSelfPid(int pid)
    {
        return pid == PeerId;
    }

    public static bool IsSelfId(long id)
    {
        return id == Id;
    }

    // LIFECYCLE METHODS
    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            Players = new Dictionary<long, NetworkPlayer>();
        }
        else
        {
            GD.PrintErr("ERROR: Network Manager already exists!");
            QueueFree();
        }
    }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += (id) => {
            GD.Print($"Received connection from Player {id}!");
            Players.Add(id, new NetworkPlayer(id, $"Player {id}"));
            OnServerConnectionsChanged?.Invoke();

            if (!_isInit)
                _isInit = true;
            
            if (IsServer)
            {

                RpcId(id, MethodName.RpcInitPlayer, new Variant[] { id });
                Rpc(
                    MethodName.RpcBroadcastPlayers,
                    new Variant[] { Players.Values.ToArray() }
                );
            }
        };
        Multiplayer.PeerDisconnected += (id) => {
            _isInit = false;

            if (IsServer)
            {
                GD.Print($"Player {id} disconnected!");
                Players.Remove(id);
                OnServerConnectionsChanged?.Invoke();
            }

            OnDisconnected?.Invoke();
        };
        Multiplayer.ConnectedToServer += () => {
            GD.Print("Connected to server!");
            OnClientConnectedToServer?.Invoke();
        };
        Multiplayer.ServerDisconnected += () => {
            GD.Print("Disconnected from server!");
        };
        Multiplayer.ConnectionFailed += () => {
            GD.Print("ERROR: Connection failed!");
        };
    }

    // NERTWORKING LIFECYCLE
    public void HostServer()
    {
        IsServer = true;
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(ServerPort, 4);
        if (error != Error.Ok)
        {
            GD.PrintErr($"ERROR: Could not start server: {error}");
            return;
        }
        // TODO(calco): Experiment with this.
        _peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        
        Multiplayer.MultiplayerPeer = _peer;
        PeerId = Multiplayer.GetUniqueId();
        GD.Print("Started hosting server! Waiting for connections ...");
        
        // TODO(calco): This should not be done here.
        Players.Add(0, new NetworkPlayer(0, "SERVER"));

        OnServerStarted?.Invoke();
    }

    public void JoinServer(string ip)
    {
        IsServer = false;
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(ip, ServerPort);
        if (error != Error.Ok)
        {
            GD.PrintErr($"ERROR: Could not start client: {error}");
            return;
        }
        // TODO(calco): Experiment with this.
        _peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

        Multiplayer.MultiplayerPeer = _peer;
        PeerId = Multiplayer.GetUniqueId();
    }

    public void Disconnect()
    {
        _peer = null;
        Multiplayer.MultiplayerPeer = null;
    }

    public void KickPlayer(int id)
    {
        Multiplayer.MultiplayerPeer.DisconnectPeer(id);
    }

    // RPC METHODS
    private static string GetRpcFormat()
    {
        string type = IsServer ? "Server" : "Client";
        return $"RPC {Id} ({type}): ";
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RpcInitPlayer(long id)
    {
        Id = id;
        GD.Print($"{GetRpcFormat()} Initialized.");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RpcBroadcastPlayers(NetworkPlayer[] players)
    {
        OnBroadcastPlayers?.Invoke(players);
    }
}