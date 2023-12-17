using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks.Dataflow;
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

    // REFERENCES
    private Timer _readyTimer;

    // GENERAL METHODS
    public Action<int> OnSetCountdown;
    public Action<long> OnPlayerSelectCharacter;

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
        _readyTimer = GetNode<Timer>("ReadyTimer");
        _readyTimer.OneShot = true;
        _readyTimer.Timeout += () => {
            GD.Print("Ready timer timeout. Broadcasting enter game...");
            Broadcast(true, (player) => {
                RpcId(player.Id, MethodName.RpcEnterGame);
            });
        };

        Multiplayer.PeerConnected += (id) => {
            if (IsServer)
            {
                GD.Print($"Server received connection from Player {id}!");
                
                string usrnam = $"Player {id}"; // short name to fit lol
                int idx = Players.Count;
                Players.Add(id, new NetworkPlayer(id, usrnam, idx));

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
        Players.Add(Id, new NetworkPlayer(Id, Username, 0));
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
    private void Broadcast(bool includeSelf, Action<NetworkPlayer> func)
    {
        foreach (NetworkPlayer player in Players.Values)
        {
            if (!includeSelf && IsSelfId(player.Id))
                continue;
            
            func(player);
        }
    }

    public void UpdatePlayerCharacter(int idx)
    {
        GD.Print($"{GetRpcFormat()} Tried updating player character!");
        RpcId(ServerPeerId, MethodName.RpcUpdatePlayerCharacter, Id, idx);
    }

    public void GoToCharacterSelect()
    {
        if (!IsServer)
        {
            GD.Print($"{GetRpcFormat()} Tried going to charcater select but is not server!");
            return;
        }

        GD.Print($"{GetRpcFormat()} Broadcasting going to character select...");
        Broadcast(true, (player) => {
            RpcId(player.Id, MethodName.RpcGoToCharacterSelect);
        });
    }

    public void AnnounceReady()
    {
        GD.Print($"{GetRpcFormat()} Tried announcing ready!");
        RpcId(ServerPeerId, MethodName.RpcPlayerReady, Id);
    }

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
    private void RpcUpdatePlayerCharacter(long id, int idx)
    {
        if (!Players.ContainsKey(id))
        {
            GD.Print($"{GetRpcFormat()} Tried updating player {id} but doesn't exist!");
            return;
        }
        
        GD.Print($"{GetRpcFormat()} Updating player {id} character!");
        Players[id].CharacterId = idx;
        OnPlayerSelectCharacter?.Invoke(id);

        if (IsServer)
        {
            Broadcast(false, (player) => {
                RpcId(player.Id, MethodName.RpcUpdatePlayerCharacter, id, idx);
            });
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSendPlayerInfo(long id, string username)
    {
        if (!Players.ContainsKey(id))
        {
            GD.Print($"{GetRpcFormat()} Adding player {id} {username} to list!");
            int idx = Players.Count;
            Players.Add(id, new NetworkPlayer(id, username, idx));
        }
        else
        {
            // Players[id] = new NetworkPlayer(id, username);
            Players[id].Id = id;
            Players[id].Username = username;
            // GD.Print($"{GetRpcFormat()} Tried adding player {id} {username} to list, but was already present!");
        }

        if (IsServer)
        {
            Broadcast(false, (player) => {
                RpcId(
                    player.Id,
                    MethodName.RpcSendPlayerInfo,
                    new Variant[] { id, username }
                );
            });
        }

        OnServerConnectionsChanged?.Invoke();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcPlayerReady(long id)
    {
        if (!Players.ContainsKey(id))
        {
            GD.PrintErr($"{GetRpcFormat()} Tried readying up player with unknown id {id}");
            return;
        }
        GD.Print($"{GetRpcFormat()} Readied up player {id}.");

        Players[id].IsReady = true;

        if (IsServer)
        {
            Broadcast(false, (player) => {
                RpcId(
                    player.Id,
                    MethodName.RpcPlayerReady,
                    new Variant[] { id }
                );
            });

            int readyCount = Players.Aggregate(0, (acc, kvp) => {
                return acc + (kvp.Value.IsReady ? 1 : 0);
            });
            
            if (readyCount == 1 || readyCount == Players.Count)
            {
                int sec = readyCount == 1 ? 60 : 5;
                if (_readyTimer.TimeLeft == 0f || _readyTimer.TimeLeft > sec)
                    _readyTimer.Start(sec);

                Broadcast(true, (player) => {
                    RpcId(
                        player.Id,
                        MethodName.RpcSetCountdown,
                        new Variant[] { sec }
                    );
                });
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcSetCountdown(int seconds)
    {
        GD.Print($"{GetRpcFormat()} Set countdown to {seconds} seconds.");
        OnSetCountdown?.Invoke(seconds);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcGoToCharacterSelect()
    {
        GD.Print($"{GetRpcFormat()} Changing scenes to Character Select!");
        GetTree().ChangeSceneToFile("res://scenes/CharacterSelect.tscn");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcEnterGame()
    {
        GD.Print($"{GetRpcFormat()} Entered game.");
        GetTree().ChangeSceneToFile("res://scenes/Gameplay.tscn");

        // TODO(calco): This countdown should be server authoritative
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