using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using KongleJam.Networking.Custom;
using HttpClient = System.Net.Http.HttpClient;

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

    public static NetworkPlayer Player => Instance.Players[Id];
    
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

    private Upnp _upnp;


    public Action<long, string, Variant> OnRpcSyncThing;

    // STATIC HELPERS 
    public static bool IsSelfPid(int pid)
    {
        return pid == PeerId;
    }

    public static bool IsSelfId(long id)
    {
        return id == Id;
    }

    private static string _lobbyCodeCache = "";
    public static async Task<string> GetLobbyCode()
    {
        if (_lobbyCodeCache.Length > 0)
            return _lobbyCodeCache;

        using HttpClient httpClient = new();
        try
        {
            HttpResponseMessage response =
                await httpClient.GetAsync("https://api.ipify.org/");

            response.EnsureSuccessStatusCode();
            if (!response.IsSuccessStatusCode)
            {
                GD.PrintErr("ERROR: Request to API was unsuccesful!");
                return "ERROR: Could not get PUBLIC IP.";
            }

            _lobbyCodeCache = await response.Content.ReadAsStringAsync();
            return _lobbyCodeCache;
        }
        catch (HttpRequestException ex)
        {
            GD.PrintErr("ERROR: Could not fetch public IP address: " + ex.Message);
            return "ERROR: Could not get PUBLIC IP.";
        }
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

    private void MapPort(int port, string protocol)
    {
        var map_res = _upnp.AddPortMapping(port, port, "Kongle Port", protocol);
        if (map_res == (int)Upnp.UpnpResult.Success)
        {
            GD.Print($"{GetRpcFormat()} Succesfully mapped ports!");
            return;
        }
        GD.PrintErr($"{GetRpcFormat()} ERROR: Failed named UPNP port mapping!");
    
        map_res = _upnp.AddPortMapping(port, port, "Kongle", protocol);
        if (map_res == (int)Upnp.UpnpResult.Success)
        {
            GD.Print($"{GetRpcFormat()} Succesfully mapped ports!");
            return;
        }
        GD.PrintErr($"{GetRpcFormat()} ERROR: Failed unnamed UPNP port mapping! Aborting...");
    }

    private void MapPorts()
    {
        var res = _upnp.Discover();
        // GD.Print($"RES: {res}, {_upnp.GetGateway().IsValidGateway()}");
        if (res != (int)Upnp.UpnpResult.Success)
        {
            GD.PrintErr($"{GetRpcFormat()} ERROR: Failed discovering UPNP clients!");
            return;
        }
    
        if (_upnp.GetGateway() == null || !_upnp.GetGateway().IsValidGateway())
        {
            GD.PrintErr($"{GetRpcFormat()} ERROR: Invalid UPNP gateway!");
            return;
        }

        MapPort(25565, "TCP");
        MapPort(25565, "UDP");
    }

    private void UnmapPorts()
    {
        _upnp.DeletePortMapping(25565, "TCP");
        _upnp.DeletePortMapping(25565, "UDP");
    }

    public override void _Ready()
    {
        _upnp = new Upnp();
        MapPorts();

        _readyTimer = GetNode<Timer>("ReadyTimer");
        _readyTimer.OneShot = true;
        _readyTimer.Timeout += () => {
            GD.Print("Ready timer timeout. Broadcasting enter game...");
           
            var rng = new RandomNumberGenerator();
            int map_idx = rng.RandiRange(0, Game.Instance.Maps.Count - 1);
            
            Broadcast(true, (player) => {
                RpcId(player.Id, MethodName.RpcEnterGame, map_idx);
            });
        };

        Multiplayer.PeerConnected += (id) => {
            if (IsServer)
            {
                GD.Print($"Server received connection from Player {id}!");
                
                string usrnam = $"Player {id}"; // short name to fit lol
                int idx = Players.Count;
                Players.Add(id, new NetworkPlayer(id, usrnam, idx, idx));

                RpcId(id, MethodName.RpcInitPlayer, new Variant[] { id, Id, idx });
                
                // Tell everyone about new guy
                Rpc(MethodName.RpcSendPlayerInfo, new Variant[] { id, usrnam, idx });
               
               // Tell new guy about everyone
                foreach (NetworkPlayer player in Players.Values)
                {
                    if (player.Id == id)
                        continue;

                    RpcId(
                        id, MethodName.RpcSendPlayerInfo,
                        new Variant[] { player.Id, player.Username, player.Index } 
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

    public override void _ExitTree()
    {
        UnmapPorts();
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
        Players.Add(Id, new NetworkPlayer(Id, Username, 0, 0));
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

    public void SyncThing(string name, Variant value)
    {
        Broadcast(false, (player) => {
            RpcId(player.Id, MethodName.RpcSyncThing, Id, name, value);
        });
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    private void RpcSyncThing(long id, string name, Variant value)
    {
        OnRpcSyncThing?.Invoke(id, name, value);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority)]
    private void RpcInitPlayer(long id, long serverId, int idx)
    {
        Id = id;
        ServerPeerId = serverId;
            
        Rpc(MethodName.RpcSendPlayerInfo, new Variant[] {Id, Username, idx});
        
        GD.Print($"{GetRpcFormat()} Initialized with index {idx}.");
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
    private void RpcSendPlayerInfo(long id, string username, int idx)
    {
        if (!Players.ContainsKey(id))
        {
            GD.Print($"{GetRpcFormat()} Adding player {id} {username} {idx} to list!");
            int localIndex = Players.Count;
            Players.Add(id, new NetworkPlayer(id, username, idx, localIndex));
        }
        else
        {
            // Players[id] = new NetworkPlayer(id, username);
            Players[id].Id = id;
            Players[id].Username = username;
            Players[id].Index = idx;
            // GD.Print($"{GetRpcFormat()} Tried adding player {id} {username} to list, but was already present!");
        }

        if (IsServer)
        {
            Broadcast(false, (player) => {
                RpcId(
                    player.Id,
                    MethodName.RpcSendPlayerInfo,
                    new Variant[] { id, username, idx }
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
                int sec = readyCount == 1 ? (Players.Count == 1 ? 5 : 60) : 5;
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
    private void RpcEnterGame(int map_idx)
    {
        GD.Print($"{GetRpcFormat()} Entered game on map {map_idx}!");
        GetTree().ChangeSceneToPacked(Game.Instance.Maps[map_idx]);
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