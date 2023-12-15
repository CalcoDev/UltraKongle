using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace KongleJam.Networking;

public class Server : Peer
{
    public Action<IPEndPoint, int, MessageType>? OnClientConnectedCallback;
    public Action<IPEndPoint, int, MessageType>? OnClientDisconnectedCallback;

    // TODO(calco): Split this into Tcp and Udp clients, although not major bug.
    private readonly Dictionary<IPEndPoint, int> _clientIds;
    private readonly Dictionary<IPEndPoint, TcpClient> _tcpClients;

    private int _currentClientId;
    private readonly TcpListener _tcpListener;
    private readonly byte[] _tcpDataBuffer;

    public Server(int receivePort) : base(receivePort)
    {
        _clientIds = new Dictionary<IPEndPoint, int>();
        _tcpClients = new Dictionary<IPEndPoint, TcpClient>();
        _tcpListener =
            new TcpListener(IPAddress.Parse("127.0.0.1"), receivePort);
        _tcpDataBuffer = new byte[1024];
    }

    public override void Start()
    {
        base.Start();

        _tcpListener.Start();
        _tcpListener.BeginAcceptTcpClient(TcpClientConnectCallback, null);
    }

    public override void Close()
    {
        // Disconnect all the clients, via TCP and UDP.
        byte[] dataBuffer = { (byte)CorePackets.Disconnect };
        foreach (var (_, client) in _tcpClients)
            SendBytesTcp(dataBuffer, client);

        foreach (var (ip, _) in _clientIds)
            SendBytes(dataBuffer, ip);

        base.Close();

        _tcpListener.Stop();
    }

    public void BroadcastBytes(byte[] data, IPEndPoint exclude,
        MessageType type)
    {
        foreach (var (ip, _) in _clientIds)
        {
            if (Equals(ip, exclude))
                continue;

            if (type == MessageType.Udp)
                SendBytes(data, ip);
            else
                SendBytesTcp(data, _tcpClients[ip]);
        }
    }

    protected override void HandleReceivedData(byte[] data, IPEndPoint sender,
        MessageType type)
    {
        if (data.Length == 0)
            return;

        switch (data[0])
        {
            case (byte)CorePackets.Connect:
            {
                if (_clientIds.TryAdd(sender, _currentClientId))
                    _currentClientId += 1;

                int id = _clientIds[sender];
                OnClientConnectedCallback?.Invoke(sender, id, type);

                byte[] sendBuffer =
                    { (byte)CorePackets.Connect, (byte)id };
                if (type == MessageType.Udp)
                    SendBytes(sendBuffer, sender);
                else
                    SendBytesTcp(sendBuffer, _tcpClients[sender]);

                break;
            }
            case (byte)CorePackets.Disconnect:
            {
                OnClientDisconnectedCallback?.Invoke(sender, _clientIds[sender],
                    type);

                // TODO(calco): Should remove, but check if both TCP and UDP.
                // _clientIds.Remove(sender);

                byte[] sendBuffer = { (byte)CorePackets.Disconnect };
                if (type == MessageType.Udp)
                    SendBytes(sendBuffer, sender);
                else
                {
                    SendBytesTcp(sendBuffer, _tcpClients[sender]);
                    // _tcpClients.Remove(sender);
                }

                break;
            }
        }

        byte[] b = new byte[data.Length - 1];
        Array.Copy(data, 1, b, 0, b.Length);

        if (PacketHandlers.TryGetValue(data[0], out var f))
            f.Invoke(b, sender, _clientIds[sender]);
    }

    private void TcpClientConnectCallback(IAsyncResult ar)
    {
        if (!Active)
            return;

        TcpClient tcpClient = _tcpListener.EndAcceptTcpClient(ar);

        IPEndPoint sender = (IPEndPoint)tcpClient.Client.RemoteEndPoint!;
        _tcpClients.TryAdd(sender, tcpClient);
        tcpClient.GetStream().BeginRead(_tcpDataBuffer, 0, 1024,
            TcpClientReceiveCallback, sender);

        _tcpListener.BeginAcceptTcpClient(TcpClientConnectCallback, null);
    }

    private void TcpClientReceiveCallback(IAsyncResult ar)
    {
        if (!Active)
            return;

        IPEndPoint sender = (IPEndPoint)ar.AsyncState!;
        TcpClientReceiveCallbackBase(ar, _tcpClients[sender].GetStream(),
            sender, _tcpDataBuffer,
            TcpClientReceiveCallback);
    }
}
