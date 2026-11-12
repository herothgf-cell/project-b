using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public static class PvpLanDiscovery
{
    public const int Port = 47777;
    const string QueryPrefix = "EungyeTaxi/lan/1/";
    const int PacketLimit = 512;
    const int PollLimit = 32;

    [Serializable]
    public sealed class Room
    {
        public string query, id, name, protocol, map;
        public int port, seconds, players;
        public bool started, open;
        [NonSerialized] public string Address;

        public bool Compatible => protocol == PvpNativeLan.Protocol && map == PvpNativeLan.MapFingerprint;
        public bool CanRejoin => PvpNativeLan.CanRejoinRoom(id, Address, (ushort)port);
    }

    sealed class Link
    {
        public IPAddress Address, Broadcast;
        public uint Mask, Network;
        public bool Contains(IPAddress address) => (ToNumber(address) & Mask) == Network;
    }

    static List<Link> LocalLinks()
    {
        var links = new List<Link> { new Link { Address = IPAddress.Loopback, Broadcast = IPAddress.Loopback, Mask = uint.MaxValue, Network = ToNumber(IPAddress.Loopback) } };
        NetworkInterface[] adapters;
        try { adapters = NetworkInterface.GetAllNetworkInterfaces(); }
        catch (NetworkInformationException) { return links; }
        foreach (var adapter in adapters)
        {
            if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel || !adapter.Supports(NetworkInterfaceComponent.IPv4)) continue;
            UnicastIPAddressInformationCollection addresses;
            try { addresses = adapter.GetIPProperties().UnicastAddresses; }
            catch (NetworkInformationException) { continue; }
            foreach (var ip in addresses)
            {
                if (ip.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip.Address) || ip.IPv4Mask == null) continue;
                uint mask = ToNumber(ip.IPv4Mask);
                if (mask == 0 || ~mask < 3) continue;
                uint network = ToNumber(ip.Address) & mask;
                uint broadcast = network | ~mask;
                links.Add(new Link {
                    Address = ip.Address, Mask = mask, Network = network,
                    Broadcast = new IPAddress(new byte[] { (byte)(broadcast >> 24), (byte)(broadcast >> 16), (byte)(broadcast >> 8), (byte)broadcast })
                });
            }
        }
        return links;
    }

    static uint ToNumber(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
    }

    static UdpClient Open(IPAddress address, int port)
    {
        var udp = new UdpClient(new IPEndPoint(address, port));
        try
        {
            udp.Ttl = 1;
            udp.Client.Blocking = false;
            return udp;
        }
        catch
        {
            udp.Dispose();
            throw;
        }
    }

    static bool Read(UdpClient udp, out byte[] packet, out IPEndPoint sender)
    {
        packet = null;
        sender = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            if (!udp.Client.Poll(0, SelectMode.SelectRead)) return false;
            packet = udp.Receive(ref sender);
            return true;
        }
        catch (SocketException)
        {
            // Windows can report an ICMP rejection when a searched port has no listener.
            return false;
        }
    }

    public sealed class Host : IDisposable
    {
        readonly UdpClient udp = Open(IPAddress.Any, Port);
        List<Link> links;
        float refreshAt;

        public void Poll(string id, string name, int gamePort, int seconds, int players, bool started, bool open)
        {
            for (int i = 0; i < PollLimit && Read(udp, out byte[] packet, out IPEndPoint sender); i++)
            {
                if (packet.Length != QueryPrefix.Length + 32) continue;
                string request = Encoding.ASCII.GetString(packet);
                if (!request.StartsWith(QueryPrefix, StringComparison.Ordinal)) continue;
                string query = request.Substring(QueryPrefix.Length);
                if (!Guid.TryParseExact(query, "N", out _)) continue;
                if (links == null || Time.realtimeSinceStartup >= refreshAt)
                {
                    links = LocalLinks();
                    refreshAt = Time.realtimeSinceStartup + 5;
                }
                if (!links.Exists(link => link.Contains(sender.Address))) continue;

                byte[] reply = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Room {
                    query = query, id = id, name = name, protocol = PvpNativeLan.Protocol, map = PvpNativeLan.MapFingerprint,
                    port = gamePort, seconds = seconds, players = players, started = started, open = open
                }));
                try { udp.Send(reply, reply.Length, sender); }
                catch (SocketException) { }
            }
        }

        public void Dispose() => udp.Dispose();
    }

    public sealed class Search : IDisposable
    {
        readonly List<(Link link, UdpClient udp)> sockets = new List<(Link, UdpClient)>();
        readonly string query = Guid.NewGuid().ToString("N");

        public Search()
        {
            byte[] packet = Encoding.ASCII.GetBytes(QueryPrefix + query);
            foreach (var link in LocalLinks())
            {
                UdpClient udp = null;
                try
                {
                    udp = Open(link.Address, 0);
                    udp.EnableBroadcast = true;
                    udp.Send(packet, packet.Length, new IPEndPoint(link.Broadcast, Port));
                    sockets.Add((link, udp));
                }
                catch (SocketException) { udp?.Dispose(); }
            }
        }

        public List<Room> ReadRooms()
        {
            var rooms = new Dictionary<string, Room>();
            foreach (var (link, udp) in sockets)
            {
                for (int i = 0; i < PollLimit && Read(udp, out byte[] packet, out IPEndPoint sender); i++)
                {
                    if (sender.Port != Port || !link.Contains(sender.Address) || packet.Length == 0 || packet.Length > PacketLimit) continue;
                    Room room;
                    try { room = JsonUtility.FromJson<Room>(Encoding.UTF8.GetString(packet)); }
                    catch (ArgumentException) { continue; }
                    if (room == null || room.query != query || !Guid.TryParseExact(room.id, "N", out _) ||
                        string.IsNullOrWhiteSpace(room.name) || room.name.Length > 24 || room.port < 1 || room.port > 65535 ||
                        room.seconds < 30 || room.seconds > 7200 || room.players < 1 || room.players > 4) continue;
                    room.Address = sender.Address.ToString();
                    if (!rooms.TryGetValue(room.id, out Room previous) || room.CanRejoin ||
                        (!previous.CanRejoin && previous.Address == "127.0.0.1" && !IPAddress.IsLoopback(sender.Address)))
                        rooms[room.id] = room;
                }
            }
            var sorted = new List<Room>(rooms.Values);
            sorted.Sort((left, right) => {
                int nameOrder = string.CompareOrdinal(left.name, right.name);
                return nameOrder != 0 ? nameOrder : string.CompareOrdinal(left.id, right.id);
            });
            return sorted;
        }

        public void Dispose()
        {
            foreach (var (_, udp) in sockets) udp.Dispose();
            sockets.Clear();
        }
    }
}
