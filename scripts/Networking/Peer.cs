using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Godot;

namespace KongleJam.Networking;

public abstract class Peer
{
    private readonly UdpClient _udpClient;

    protected readonly IPEndPoint ReceiveEndPoint;
    protected IPEndPoint SendEndPoint;

    public bool Active { get; private set; } = false;

    public Action<int>? OnStartedCallback;
    public Action? OnClosedCallback;
    public Action<byte[], IPEndPoint, MessageType>? OnDataReceivedCallback;

    protected readonly Dictionary<byte, Action<byte[], IPEndPoint, int>> PacketHandlers;

    public IPEndPoint GetSendEndPoint()
    {
        return SendEndPoint;
    }

    public IPEndPoint GetReceiveEndPoint()
    {
        return ReceiveEndPoint;
    }

    public Peer(int receivePort)
    {
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, receivePort));
        ReceiveEndPoint = (IPEndPoint)_udpClient.Client.LocalEndPoint!;

        // TODO(calco): Figure out a better default.
        SendEndPoint = new IPEndPoint(IPAddress.Any, receivePort);

        OnStartedCallback = null;
        OnClosedCallback = null;
        OnDataReceivedCallback = null;

        PacketHandlers = new Dictionary<byte, Action<byte[], IPEndPoint, int>>();
        OnDataReceivedCallback = HandleReceivedData;
    }

    public void AddPacketHandler(byte packet,
        Action<byte[], IPEndPoint, int> handler)
    {
        PacketHandlers.Add(packet, handler);
    }

    public void SetSendEndPoint(IPEndPoint ipEndPoint)
    {
        SendEndPoint = ipEndPoint;
    }

    public virtual void Start()
    {
        Active = true;
        OnStartedCallback?.Invoke(ReceiveEndPoint.Port);

        _udpClient.BeginReceive(UdpClientReceiveCallback, null);
    }

    public virtual void Close()
    {
        Active = false;
        OnClosedCallback?.Invoke();

        _udpClient.Close();
    }

    public void SendBytes(byte[] data)
    {
        _udpClient.Send(data, SendEndPoint);
    }

    public void SendBytes(byte[] data, IPEndPoint endPoint)
    {
        SetSendEndPoint(endPoint);
        _udpClient.Send(data, SendEndPoint);
    }

    public void SendBytesTcp(byte[] data, TcpClient client)
    {
        client.GetStream().Write(data);
    }

    private void UdpClientReceiveCallback(IAsyncResult ar)
    {
        if (!Active)
            return;

        IPEndPoint endPoint = null!;
        byte[] data = _udpClient.EndReceive(ar, ref endPoint!);

        OnDataReceivedCallback?.Invoke(data, endPoint, MessageType.Udp);

        if (this is Server || data[0] != (byte)CorePackets.Disconnect)
            _udpClient.BeginReceive(UdpClientReceiveCallback, null);
    }

    protected void TcpClientReceiveCallbackBase(IAsyncResult ar,
        NetworkStream stream, IPEndPoint sender, byte[] buffer,
        AsyncCallback f)
    {
        int bytesRead = stream.EndRead(ar);
        if (bytesRead >= 0)
        {
            byte[] data = new byte[bytesRead];
            Array.Copy(buffer, 0, data, 0, bytesRead);
            OnDataReceivedCallback?.Invoke(data,
                sender, MessageType.Tcp);
        }

        if (bytesRead <= 0 || buffer[0] != (byte)CorePackets.Disconnect)
            stream.BeginRead(buffer, 0, 1024,
                f, sender);
    }

    protected abstract void HandleReceivedData(byte[] data, IPEndPoint sender,
        MessageType type);
}