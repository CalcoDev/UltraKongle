using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public static string Username { get; private set; } = "DEFAULT";
    public static int PeerId { get; private set; } = -1;
    public static long ServerPeerId { get; private set; } = -1;
    
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

    public static string GetLobbyCode()
    {
        string hostname = Dns.GetHostName();
        string ip = Dns.GetHostEntry(hostname).AddressList[0].ToString();
        return ip;
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
            if (IsServer)
            {
                GD.Print($"Server received connection from Player {id}!");
                
                string usrnam = $"Player {id}"; // short name to fit lol
                Players.Add(id, new NetworkPlayer(id, usrnam));

                RpcId(id, MethodName.RpcInitPlayer, new Variant[] { id, Id });
                
                // Tell everyone about new guy
                Rpc(MethodName.RpcSendPlayerInfo, new Variant[] { id, usrnam });
               
               // Tell new guy about everyone
                foreach (NetworkPlayer player in Players.Values)
                {
                    if (player.Id == id)
                        continue;

                    RpcId(
                        id, MethodName.RpcSendPlayerInfo,
                        new Variant[] { player.Id, player.Username } 
                    );
                }
            }
            OnServerConnectionsChanged?.Invoke();
            
            // TODO(calco): Figure out where to put this
            if (!_isInit)
                _isInit = true;
            
        };
        Multiplayer.PeerDisconnected += (id) => {
            if (IsServer)
            {
                GD.Print($"Player {id} disconnected!");
                
                Players.Remove(id);
                
                // Tell everyone about old guy
                Rpc(MethodName.RpcKickPlayer, new Variant[] { id });
            }

            if (id == ServerPeerId)
                Players.Clear();

            OnServerConnectionsChanged?.Invoke();
        };
        Multiplayer.ConnectedToServer += () => {
            GD.Print($"Connected to server!");

            OnServerConnectionsChanged?.Invoke();
            OnClientConnectedToServer?.Invoke();
        };
        Multiplayer.ServerDisconnected += () => {
            GD.Print("Disconnected from server!");
            
            OnServerConnectionsChanged?.Invoke();
            OnDisconnected?.Invoke();
        };
        Multiplayer.ConnectionFailed += () => {
            GD.PrintErr("ERROR: Connection failed!");
        };
    }

    // NERTWORKING LIFECYCLE
    public void HostServer(string username)
    {
        IsServer = true;
        _isInit = false;
        
        Players.Clear();
        
        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateServer(ServerPort, 4);
        if (error != Error.Ok)
        {
            GD.PrintErr($"ERROR: Could not start server: {error}");
            return;
        }
        _peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);
        
        Multiplayer.MultiplayerPeer = _peer;
        PeerId = Multiplayer.GetUniqueId();
        GD.Print("Started hosting server! Waiting for connections ...");
        
        // TODO(calco): This should not be done here.
        Id = PeerId;
        ServerPeerId = PeerId;
        Username = username.Length == 0 ? "SERVER" : username;
        Players.Add(Id, new NetworkPlayer(Id, Username));
        OnServerConnectionsChanged?.Invoke();

        OnServerStarted?.Invoke();
    }

    public void JoinServer(string ip, string username)
    {
        Username = username;
       
        IsServer = false;
        _isInit = false;

        Players.Clear();

        _peer = new ENetMultiplayerPeer();
        var error = _peer.CreateClient(ip, ServerPort);
        if (error != Error.Ok)
        {
            GD.PrintErr($"ERROR: Could not start client: {error}");
            return;
        }
        _peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

        Multiplayer.MultiplayerPeer = _peer;
        PeerId = Multiplayer.GetUniqueId();
    }

    public void Disconnect()
    {
        Players.Clear();
        
        if (IsServer)
        {
            GD.Print($"{GetRpcFormat()} Stopping server ...");
            foreach (NetworkPlayer player in Players.Values)
            {
                if (IsSelfId(player.Id))
                    continue;

                Multiplayer.MultiplayerPeer.DisconnectPeer((int) player.Id);
            }
            
            GD.Print($"{GetRpcFormat()} Stopped server!");
            OnDisconnected?.Invoke();
            OnServerConnectionsChanged?.Invoke();

            // TODO(calco): Properly delete and close the port after use.
            GetTree().CreateTimer(0.1f).Timeout += () => {
                Multiplayer.MultiplayerPeer.Close();
                _peer = null;
                Multiplayer.MultiplayerPeer = null;
            };
        }
        else
        {
            GD.Print($"{GetRpcFormat()} Sending disconnect packet to {ServerPeerId}!");
            RpcId(ServerPeerId, MethodName.RpcDisconnectPlayer, new Variant[] { Id });
        }
    }

    public void KickPlayer(long id)
    {
        if (!IsServer)
            return;

        // RpcId(ServerPeerId, MethodName.RpcDisconnectPlayer, new Variant[] { id });
        RpcDisconnectPlayer(id);
    }

    // RPC METHOD HALPERS

    // RPC METHODS
    public static string GetRpcFormat()
    {
        string type = IsServer ? "Server" : "Client";
        return $"RPC {Id} ({type}): ";
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RpcInitPlayer(long id, long serverId)
    {
        Id = id;
        ServerPeerId = serverId;
            
        Rpc(MethodName.RpcSendPlayerInfo, new Variant[] {Id, Username});
        
        GD.Print($"{GetRpcFormat()} Initialized.");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSendPlayerInfo(long id, string username)
    {
        if (!Players.ContainsKey(id))
        {
            GD.Print($"{GetRpcFormat()} Adding player {id} {username} to list!");
            Players.Add(id, new NetworkPlayer(id, username));
        }
        else
        {
            Players[id] = new NetworkPlayer(id, username);
            // GD.Print($"{GetRpcFormat()} Tried adding player {id} {username} to list, but was already present!");
        }

        if (IsServer)
        {
            foreach (NetworkPlayer player in Players.Values)
            {
                if (IsSelfId(player.Id))
                    continue;
                
                RpcId(player.Id, MethodName.RpcSendPlayerInfo, new Variant[] { id, username });
            }
        }

        OnServerConnectionsChanged?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RpcKickPlayer(long id)
    {
        if (Players.Remove(id))
            GD.Print($"{GetRpcFormat()} Removed player {id} from players.");
        else
            GD.Print($"{GetRpcFormat()} Tried removing player {id} from players, but was not present.");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcDisconnectPlayer(long id)
    {
        if (IsServer)
        {
            GD.Print($"{GetRpcFormat()} Disconnecting player {id}");
            Multiplayer.MultiplayerPeer.DisconnectPeer((int) id);
        }
        else
        {
            GD.Print($"{GetRpcFormat()} Received disconnect message even though not server?!?!?!");
        }
    }
}