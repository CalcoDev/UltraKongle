using System;
using System.Net;
using System.Net.Sockets;

namespace KongleJam.Networking;

public class Client : Peer
{
    public int Id { get; private set; } = -1;

    public bool UdpConnected { get; private set; } = false;
    public bool TcpConnected { get; private set; } = false;
    public bool Connected => UdpConnected && TcpConnected;

    public Action<int, MessageType>? OnConnectedCallback;
    public Action<int, MessageType>? OnDisconnectedCallback;

    private readonly TcpClient _tcpClient;
    private readonly byte[] _tcpDataBuffer;

    public Client(int receivePort) : base(receivePort)
    {
        IPAddress addr = IPAddress.Parse("127.0.0.1");
        IPEndPoint tcpIp = new IPEndPoint(addr, ReceiveEndPoint.Port);
        _tcpClient = new TcpClient(tcpIp);

        _tcpDataBuffer = new byte[1024];
    }

    public override void Start()
    {
        base.Start();

        _tcpClient.Connect(SendEndPoint);
        _tcpClient.GetStream().BeginRead(_tcpDataBuffer, 0, 1024,
            TcpClientReceiveCallback, null);
    }

    public void Connect(string ip, int port)
    {
        SetSendEndPoint(new IPEndPoint(IPAddress.Parse(ip), port));
        Start();

        SendBytes(new[] { (byte)CorePackets.Connect });
        SendBytesTcp(new[] { (byte)CorePackets.Connect });
    }

    public override void Close()
    {
        base.Close();

        _tcpClient.Close();
    }

    public void Disconnect()
    {
        SendBytes(new[] { (byte)CorePackets.Disconnect });
        SendBytesTcp(new[] { (byte)CorePackets.Disconnect });
    }

    public void SendBytesTcp(byte[] data)
    {
        NetworkStream networkStream = _tcpClient.GetStream();
        networkStream.Write(data);
    }

    protected override void HandleReceivedData(byte[] data, IPEndPoint sender,
        MessageType type)
    {
        switch (data[0])
        {
            case (byte)CorePackets.Connect:
                Id = data[1];
                if (type == MessageType.Udp)
                    UdpConnected = true;
                else
                    TcpConnected = true;
                OnConnectedCallback?.Invoke(Id, type);
                break;
            case (byte)CorePackets.Disconnect:
                if (type == MessageType.Udp)
                    UdpConnected = false;
                else
                    TcpConnected = false;
                OnDisconnectedCallback?.Invoke(Id, type);
                break;
        }

        byte[] b = new byte[data.Length - 1];
        Array.Copy(data, 1, b, 0, b.Length);

        if (PacketHandlers.TryGetValue(data[0], out var f))
            f.Invoke(b, sender, Id);
    }

    private void TcpClientReceiveCallback(IAsyncResult ar)
    {
        if (!Active)
            return;

        IPEndPoint sender = (IPEndPoint)_tcpClient.Client.RemoteEndPoint!;
        TcpClientReceiveCallbackBase(ar, _tcpClient.GetStream(), sender,
            _tcpDataBuffer, TcpClientReceiveCallback);
    }
}