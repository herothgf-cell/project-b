#requires -Version 7.4
[CmdletBinding()]
param(
    [uri]$Endpoint = 'wss://ws.postman-echo.com/raw',
    [ValidateSet('Both', 'Wired', 'WiFi')] [string]$Network = 'Both',
    [ValidateRange(1, 60)] [int]$TimeoutSeconds = 10
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows -or $Endpoint.Scheme -ne 'wss' -or $Endpoint.Port -ne 443) {
    throw 'Windows PowerShell 7.4+ and a WSS endpoint on TCP 443 are required.'
}

Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class PvpRelayNetworkProbe
{
    public static async Task<string> Check(Uri endpoint, int index, int timeoutSeconds)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var handler = new SocketsHttpHandler { UseProxy = false };
        using var http = new HttpMessageInvoker(handler, disposeHandler: false);
        using var websocket = new ClientWebSocket();
        handler.ConnectCallback = async (context, cancellation) =>
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Winsock IP_UNICAST_IF takes the IPv4 interface index in network byte order.
                socket.SetSocketOption(SocketOptionLevel.IP, (SocketOptionName)31, IPAddress.HostToNetworkOrder(index));
                await socket.ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, cancellation);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        };

        string stage = "TCP/TLS/WSS handshake";
        try
        {
            await websocket.ConnectAsync(endpoint, http, timeout.Token);
            stage = "WSS echo";
            string expected = "pvp-connectivity-" + Guid.NewGuid().ToString("N");
            await websocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(expected)), WebSocketMessageType.Text, true, timeout.Token);
            var buffer = new byte[128];
            using var reply = new MemoryStream();
            WebSocketReceiveResult message;
            do
            {
                message = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                if (message.MessageType != WebSocketMessageType.Text || reply.Length + message.Count > buffer.Length)
                    return "FAIL WSS echo: unexpected reply";
                reply.Write(buffer, 0, message.Count);
            } while (!message.EndOfMessage);
            return Encoding.UTF8.GetString(reply.ToArray()) == expected ? "PASS WSS echo" : "FAIL WSS echo: text mismatch";
        }
        catch (Exception error)
        {
            return $"FAIL {stage}: {error.GetBaseException().Message}";
        }
    }
}
'@

$adapters = [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
    Where-Object {
        $_.OperationalStatus -eq 'Up' -and
        $_.NetworkInterfaceType -in @('Ethernet', 'GigabitEthernet', 'FastEthernetT', 'Wireless80211') -and
        ($_.GetIPProperties().GatewayAddresses.Address.AddressFamily -contains 'InterNetwork') -and
        ($Network -eq 'Both' -or (($_.NetworkInterfaceType -eq 'Wireless80211') -eq ($Network -eq 'WiFi')))
    }

$checks = foreach ($adapter in $adapters) {
    $index = $adapter.GetIPProperties().GetIPv4Properties().Index
    [pscustomobject]@{
        Name = $adapter.Name
        IsWireless = $adapter.NetworkInterfaceType -eq 'Wireless80211'
        Task = [PvpRelayNetworkProbe]::Check($Endpoint, $index, $TimeoutSeconds)
    }
}

Write-Output "Testing $($Endpoint.DnsSafeHost):443 using the IPv4 route of each adapter."
$results = foreach ($check in $checks) {
    $result = $check.Task.GetAwaiter().GetResult()
    Write-Host "$($check.Name) - $result"
    [pscustomobject]@{ IsWireless = $check.IsWireless; Passed = $result -eq 'PASS WSS echo' }
}

$passedWireless = $results | Where-Object { $_.IsWireless -and $_.Passed }
$passedWired = $results | Where-Object { -not $_.IsWireless -and $_.Passed }
if (($Network -ne 'Wired' -and -not $passedWireless) -or ($Network -ne 'WiFi' -and -not $passedWired)) {
    Write-Output "Required network check did not pass: $Network. Check that the selected adapter is connected."
    exit 1
}
exit 0
