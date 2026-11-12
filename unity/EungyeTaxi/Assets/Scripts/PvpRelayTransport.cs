using System;
using Unity.Netcode;
using UnityEngine;

public sealed class PvpRelayTransport : NetworkTransport
{
    const byte Arrived = 1, Left = 2, Data = 3, Started = 4;
    const int HeaderSize = 5;
    public const int MaxBatchSize = (PvpRelaySocket.MaxPacket - HeaderSize) & ~7;
    PvpRelaySocket socket;
    bool host, initialConnect, reportedEnd;

    public void Configure(PvpRelaySocket connection) => socket = connection;

    public override ulong ServerClientId => 0;
    public override void Initialize(NetworkManager networkManager = null) { }
    public override bool StartServer()
    {
        host = true;
        return socket != null && !socket.Ended;
    }
    public override bool StartClient()
    {
        initialConnect = socket != null && !socket.Ended;
        return initialConnect;
    }

    public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
    {
        receiveTime = Time.realtimeSinceStartup;
        clientId = ServerClientId;
        payload = default;
        if (initialConnect)
        {
            initialConnect = false;
            return NetworkEvent.Connect;
        }
        if (socket == null) return NetworkEvent.Nothing;
        if (socket.TryRead(out byte[] message))
        {
            clientId = (uint)message[1] | ((uint)message[2] << 8) | ((uint)message[3] << 16) | ((uint)message[4] << 24);
            switch (message[0])
            {
                case Arrived when host && clientId != 0 && message.Length == HeaderSize: return NetworkEvent.Connect;
                case Left when host && clientId != 0 && message.Length == HeaderSize: return NetworkEvent.Disconnect;
                case Data when host == (clientId != 0):
                    payload = new ArraySegment<byte>(message, HeaderSize, message.Length - HeaderSize);
                    return NetworkEvent.Data;
                default:
                    socket.Dispose();
                    break;
            }
        }
        if (!socket.Ended || reportedEnd) return NetworkEvent.Nothing;
        reportedEnd = true;
        clientId = ServerClientId;
        return host ? NetworkEvent.TransportFailure : NetworkEvent.Disconnect;
    }

    public override void Send(ulong clientId, ArraySegment<byte> payload, NetworkDelivery delivery) => SendFrame(Data, clientId, payload);
    public override void DisconnectRemoteClient(ulong clientId) => SendFrame(Left, clientId, default);
    public void MarkStarted() => SendFrame(Started, ServerClientId, default);

    void SendFrame(byte kind, ulong id, ArraySegment<byte> payload)
    {
        if (socket == null || socket.Ended || id > uint.MaxValue) return;
        var frame = new byte[HeaderSize + payload.Count];
        frame[0] = kind;
        for (int i = 0; i < 4; i++) frame[1 + i] = (byte)(id >> (8 * i));
        if (payload.Count != 0) Buffer.BlockCopy(payload.Array, payload.Offset, frame, HeaderSize, payload.Count);
        socket.Send(frame);
    }

    public override ulong GetCurrentRtt(ulong clientId) => 0;
    public override void DisconnectLocalClient() => Shutdown();
    public override void Shutdown()
    {
        socket?.Dispose();
        socket = null;
    }
    void OnDestroy() => Shutdown();
}
