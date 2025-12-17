using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;

[Serializable]
public enum PlayerConnectionType
{
    Player,
    Spectator
}

public class TCPServer : INetwork
{
    public const int MAX_PLAYERS = 3;
    private readonly Dictionary<Socket, PlayerConnectionType> _connectionTypes = new();
    
    public bool IsServer => true;
    public bool IsRunning { get; private set; }
    public string LocalName { get; set; }
    public int Port { get; set; } = 9050;

    public event Action<string> OnLog;
    public event Action<string> OnChatMessage;
    public event Action<string> OnSystemMessage;
    public event Action<NetMsg> OnMessage;

    Socket _listen;
    readonly List<Socket> _clients = new();
    readonly Dictionary<Socket, List<byte>> _rx = new();

    //Start the server
    public void StartServer(string serverName)
    {
        LocalName = serverName;
        _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listen.Bind(new IPEndPoint(IPAddress.Any, Port));
        _listen.Listen(32);
        _listen.Blocking = false;
        IsRunning = true;
        OnSystemMessage?.Invoke($"Server '{LocalName}' listening {Port}");
    }
    //Start the client
    public void StartClient(string ip, string clientName) { }
    //Receive TCP data and process network messages
    public void Tick()
    {
        if (!IsRunning) return;

        // Accept
        if (_listen != null && _listen.Poll(0, SelectMode.SelectRead))
        {
            try
            {
                var s = _listen.Accept();
                s.Blocking = false;
                _clients.Add(s);
                _rx[s] = new List<byte>(8192);

                var playerCount = _connectionTypes.Values.Count(x => x == PlayerConnectionType.Player);
                var connectionType = playerCount < MAX_PLAYERS ? 
                    PlayerConnectionType.Player : 
                    PlayerConnectionType.Spectator;
                
                _connectionTypes[s] = connectionType;  

                string role = connectionType == PlayerConnectionType.Spectator ? "SPECTATOR" : "PLAYER";
                SendRaw(s, new NetMsg { op = NetOperation.HELLO, payload = role });

                string welcomeMsg = $"WELCOME {LocalName}";
                
                SendRaw(s, new NetMsg { op = NetOperation.SYSTEM, payload = welcomeMsg });
                OnSystemMessage?.Invoke(
                    $"Client {s.RemoteEndPoint} connected as {connectionType}. " +
                    $"Players: {_connectionTypes.Count(x => x.Value == PlayerConnectionType.Player)}/{MAX_PLAYERS}"
                );
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); }
        }

        // Read
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            try
            {
                if (!c.Poll(0, SelectMode.SelectRead)) continue;
                if (c.Available == 0) { Disconnect(i, c, "Closed"); continue; }

                byte[] tmp = new byte[Math.Min(8192, c.Available)];
                int n = c.Receive(tmp, tmp.Length, SocketFlags.None);
                if (n <= 0) continue;

                var buf = _rx[c];
                buf.AddRange(new ArraySegment<byte>(tmp, 0, n));

                while (NetCodec.TryDecodeTcp(ref buf, out var msg))
                {
                    Route(c, msg);
                }
            }
            catch { Disconnect(i, c, "Error"); }
        }
    }
    // Process and route received messages
    void Route(Socket from, NetMsg msg)
    {
        if (_connectionTypes.TryGetValue(from, out var t) && t == PlayerConnectionType.Spectator && (msg.op == NetOperation.MG_READY || msg.op == NetOperation.UPGRADE_PICKED))
        {
            return; // we silently ignore it
        }

        Broadcast(msg);

        switch (msg.op)
        {
            case NetOperation.CHAT:
                OnChatMessage?.Invoke(msg.payload);
                break;
            case NetOperation.SYSTEM:
                OnSystemMessage?.Invoke("Server: " + msg.payload);
                break;
        }
        OnMessage?.Invoke(msg);
    }
    //Send messages to all clients via TCP
    void Broadcast(NetMsg m)
    {
        var bytes = NetCodec.Encode(m, NetTransport.TCP);
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            try { c?.Send(bytes); }
            catch { Disconnect(i, c, "Send fail"); }
        }
    }
    //Send messages to the specified client via TCP
    void SendRaw(Socket c, NetMsg m)
    {
        try { c?.Send(NetCodec.Encode(m, NetTransport.TCP)); }
        catch (Exception e) { OnLog?.Invoke(e.Message); }
    }
    // Disconnect client
    void Disconnect(int idx, Socket c, string why)
    {
        OnSystemMessage?.Invoke($"Client {c?.RemoteEndPoint} disconnected: {why}");
        try { c?.Shutdown(SocketShutdown.Both); } catch { }
        try { c?.Close(); } catch { }
        _clients.RemoveAt(idx);
        _rx.Remove(c);
        _connectionTypes.Remove(c);
    }
    // Stop the server
    public void Stop()
    {
        IsRunning = false;
        for (int i = _clients.Count - 1; i >= 0; i--) { try { _clients[i]?.Shutdown(SocketShutdown.Both); } catch { } try { _clients[i]?.Close(); } catch { } }
        _clients.Clear(); _rx.Clear();
        try { _listen?.Close(); } catch { }
        _listen = null;
        OnSystemMessage?.Invoke("Server stopped");
    }
    //Send chat message
    public void Send(string text) {
        OnChatMessage?.Invoke($"{LocalName}: {text}");
        SendMessage(NetOperation.CHAT, $"{LocalName}: {text}");

    }
    // Send generic network messageK
    public void SendMessage(NetOperation op, string payload)
    {
        Broadcast(new NetMsg { op = op, payload = payload });
    }
}
