using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class PvpRelayRoom
{
    public string id, name, protocol, map;
    public int players, seconds;
    public bool started;

    public bool Valid => Guid.TryParseExact(id, "N", out _) && !string.IsNullOrWhiteSpace(name) &&
                         players >= 1 && players <= 4 && seconds >= 30 && seconds <= 7200;
    public bool Compatible => protocol == PvpNativeLan.Protocol && map == PvpNativeLan.MapFingerprint;
}

[Serializable]
public sealed class PvpRelayRoomList
{
    public PvpRelayRoom[] rooms;
}

public sealed class PvpRelayClient
{
    [Serializable] sealed class Settings { public string url, accessToken; }

    readonly Uri endpoint;
    readonly string authorization;

    PvpRelayClient(Uri endpoint, string token)
    {
        this.endpoint = endpoint;
        authorization = "Bearer " + token;
    }

    public static PvpRelayClient Load()
    {
        var settings = new Settings();
        string file = Path.Combine(Application.persistentDataPath, "pvp-relay.local.json");
        if (File.Exists(file)) settings = JsonUtility.FromJson<Settings>(File.ReadAllText(file)) ?? settings;
        string url = Environment.GetEnvironmentVariable("PVP_RELAY_URL") ?? settings.url;
        string token = Environment.GetEnvironmentVariable("PVP_RELAY_ACCESS_TOKEN") ?? settings.accessToken;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri endpoint) ||
            (endpoint.Scheme != "https" && !(endpoint.IsLoopback && endpoint.Scheme == "http")) ||
            (!endpoint.IsLoopback && endpoint.Port != 443) || endpoint.UserInfo.Length != 0 ||
            endpoint.AbsolutePath != "/" || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 ||
            string.IsNullOrEmpty(token) || token.Length < 24 || token.Length > 256)
            throw new InvalidOperationException("PvP 중계 주소와 접속 설정을 확인하세요.");
        foreach (char character in token)
            if (character < '!' || character > '~')
                throw new InvalidOperationException("PvP 중계 접속 설정을 확인하세요.");
        return new PvpRelayClient(endpoint, token);
    }

    public string RoomKey(string id) => new Uri(endpoint, "join/" + id).AbsoluteUri;

    public UnityWebRequest List()
    {
        var request = UnityWebRequest.Get(new Uri(endpoint, "rooms"));
        request.timeout = 10;
        request.redirectLimit = 0;
        request.SetRequestHeader("Authorization", authorization);
        return request;
    }

    public async Task<PvpRelaySocket> Open(string roomId, string name, int seconds, string map, CancellationToken cancellation)
    {
        bool host = roomId == null;
        if (!host && !Guid.TryParseExact(roomId, "N", out _)) throw new ArgumentException("방 정보가 올바르지 않습니다.");
        var uri = new UriBuilder(new Uri(endpoint, host ? "host" : "join/" + roomId));
        uri.Scheme = endpoint.Scheme == "https" ? "wss" : "ws";
        uri.Port = endpoint.Port;
        var socket = new ClientWebSocket();
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                socket.Options.SetRequestHeader("Authorization", authorization);
                if (host)
                {
                    socket.Options.SetRequestHeader("X-Pvp-Name", Uri.EscapeDataString(name));
                    socket.Options.SetRequestHeader("X-Pvp-Protocol", PvpNativeLan.Protocol);
                    socket.Options.SetRequestHeader("X-Pvp-Map", map);
                    socket.Options.SetRequestHeader("X-Pvp-Seconds", seconds.ToString(CultureInfo.InvariantCulture));
                }
                await socket.ConnectAsync(uri.Uri, timeout.Token).ConfigureAwait(false);
                byte[] greeting = await PvpRelaySocket.Read(socket, new byte[4096], WebSocketMessageType.Text, 4096, timeout.Token).ConfigureAwait(false);
                if (greeting == null) throw new IOException("방이 닫혔습니다.");
                return new PvpRelaySocket(socket, Encoding.UTF8.GetString(greeting));
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}

public sealed class PvpRelaySocket : IDisposable
{
    public const int MaxPacket = 1024 * 1024 + 65536;
    const int MaxQueue = 4 * 1024 * 1024;

    readonly ClientWebSocket socket;
    readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    readonly ConcurrentQueue<byte[]> inbound = new ConcurrentQueue<byte[]>();
    readonly ConcurrentQueue<byte[]> outbound = new ConcurrentQueue<byte[]>();
    readonly SemaphoreSlim writable = new SemaphoreSlim(0);
    int incomingBytes, outgoingBytes, stopped;
    public string Greeting { get; }
    public bool Ended => Volatile.Read(ref stopped) != 0;

    public PvpRelaySocket(ClientWebSocket socket, string greeting)
    {
        this.socket = socket;
        Greeting = greeting;
        CancellationToken token = lifetime.Token;
        _ = Run(token);
    }

    public void Send(byte[] message)
    {
        if (Ended) return;
        if (message.Length > MaxPacket || Interlocked.Add(ref outgoingBytes, message.Length) > MaxQueue)
        {
            Dispose();
            return;
        }
        outbound.Enqueue(message);
        writable.Release();
    }

    public bool TryRead(out byte[] message)
    {
        if (!inbound.TryDequeue(out message)) return false;
        Interlocked.Add(ref incomingBytes, -message.Length);
        return true;
    }

    async Task Run(CancellationToken token)
    {
        await Task.WhenAll(Receive(token), Write(token)).ConfigureAwait(false);
        lifetime.Dispose();
    }

    async Task Receive(CancellationToken token)
    {
        var buffer = new byte[16384];
        try
        {
            while (!token.IsCancellationRequested)
            {
                byte[] message = await Read(socket, buffer, WebSocketMessageType.Binary, MaxPacket, token).ConfigureAwait(false);
                if (message == null || message.Length < 5 || Interlocked.Add(ref incomingBytes, message.Length) > MaxQueue) break;
                inbound.Enqueue(message);
            }
        }
        catch (Exception) { }
        finally { Dispose(); }
    }

    async Task Write(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await writable.WaitAsync(token).ConfigureAwait(false);
                if (!outbound.TryDequeue(out byte[] message)) continue;
                await socket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, token).ConfigureAwait(false);
                Interlocked.Add(ref outgoingBytes, -message.Length);
            }
        }
        catch (Exception) { }
        finally { Dispose(); }
    }

    internal static async Task<byte[]> Read(ClientWebSocket socket, byte[] buffer, WebSocketMessageType expected, int max, CancellationToken token)
    {
        WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
        if (result.MessageType == WebSocketMessageType.Close) return null;
        if (result.MessageType != expected || result.Count > max) throw new IOException("잘못된 중계 메시지입니다.");
        if (result.EndOfMessage)
        {
            var message = new byte[result.Count];
            Buffer.BlockCopy(buffer, 0, message, 0, result.Count);
            return message;
        }
        using (var message = new MemoryStream())
        {
            do
            {
                message.Write(buffer, 0, result.Count);
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close) return null;
                if (result.MessageType != expected || message.Length + result.Count > max)
                    throw new IOException("잘못된 중계 메시지입니다.");
            } while (!result.EndOfMessage);
            message.Write(buffer, 0, result.Count);
            return message.ToArray();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0) return;
        lifetime.Cancel();
        socket.Dispose();
    }
}
