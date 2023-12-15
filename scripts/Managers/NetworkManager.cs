using System;
using System.Collections.Generic;
using Godot;
using KongleJam.Networking;
using KongleJam.Networking.Custom;

namespace KongleJam.Managers;

public partial class NetworkManager : Node
{
    public const int ServerPort = 25565;
    public static int ClientPort { get; private set; } = 0;

    public static NetworkManager Instance { get; private set; }

    private readonly Dictionary<int, NetworkPlayer> _players = new();

    private Server? _server;
    private Client? _client;

    public override void _EnterTree()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            GD.PrintErr("ERROR: Network Manager already exists!");
            QueueFree();
        }
    }

    public NetworkPlayer GetPlayerData(int id)
    {
        return _players[id];
    }

    public bool IsSelf(int id)
    {
        return _client.Id == id;
    }


    private string _cachedIp;
    private string GetPublicIp()
    {
        // TODO(calco): Make this run better.
        if (_cachedIp == null)
            _cachedIp = new System.Net.WebClient().DownloadString("https://api.ipify.org");
        
        return _cachedIp;
    }

    public string GetLobbyCopy() {
        if (_server != null && _server.Active)
            return GetPublicIp();
        else if (_client != null && _client.Active)
            return _client.GetSendEndPoint().Address.ToString();
        else
            return "ayo cannot do that";
    }

    public void StartServer(Action onStartedCallback)
    {
        // if (_server != null && _server.Active)
        //     _server.Close();
        
        _server = new Server(ServerPort);
        _server.OnStartedCallback += p => {
            GD.Print($"Started server on port: {p}!");
            _players.Clear();
            onStartedCallback();

            GetTree().CreateTimer(0.5f).Connect(
                Timer.SignalName.Timeout, 
                new Callable(Instance, MethodName.StartServerClient)
            );
        };
        _server.OnClientConnectedCallback += (ip, id, type) => {
            GD.Print($"{ip} connected via {type} and got id {id}!");
            _players.TryAdd(id,
                new NetworkPlayer { Username = $"Client {id}" }
            );
        };
        _server.OnClientDisconnectedCallback += (ip, id, type) => {
            GD.Print($"{ip} disconnected via {type}!");
        };
        _server.OnClosedCallback += () => {
            GD.Print("Server stopped!");
        };

        _server.Start();
    }
    
    private void StartServerClient()
    {
        GD.Print("Starting server client...");
        StartClient("127.0.0.1", null);
    }

    public void StartClient(string ip, Action onConnectedCallback)
    {
        if (_client != null && _client.Active)
            _client.Disconnect();

        _client = new Client(0);
        _client.OnStartedCallback += p => {
            ClientPort = p;
            GD.Print($"Client started with port: {p}!");
        };
        _client.OnConnectedCallback += (id, type) => {
            GD.Print($"Connected via {type} and got id {id}!");
            onConnectedCallback?.Invoke();
        };
        _client.OnClosedCallback += () => {
            GD.Print("Closed client!");
        };
        _client.OnDisconnectedCallback += (_, type) => {
            GD.Print($"Disconnected via {type}.");
        };

        // string ip, int port
        GD.Print($"Client connecting to ip {ip} and port {ServerPort}!");
        _client.Connect(ip, ServerPort);
    }

    public void Disconnect(Action onDisconnect)
    {
        if (_client != null)
        {
            _client.OnDisconnectedCallback += (_, _) => onDisconnect();
            _client.Disconnect();
        }
        
        if (_server != null)
        {
            _server.OnClosedCallback += () => onDisconnect();
            _server.Close();
        }
    }
}