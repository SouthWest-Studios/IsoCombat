using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class TCPClient : INetwork
{
    public bool IsServer => false;
    public bool IsRunning { get; private set; }
    public string LocalName { get; set; }
    public int Port { get; set; } = 9050;

    public event Action<string> OnLog;
    public event Action<string> OnChatMessage;
    public event Action<string> OnSystemMessage;
    public event Action<NetMsg> OnMessage;

    Socket _sock;
    List<byte> _rx = new(8192);

    public void StartServer(string n) { }

    public void StartClient(string serverIp, string clientName)
    {
        LocalName = clientName;
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _sock.Connect(IPAddress.Parse(serverIp), Port);
        _sock.Blocking = false;
        IsRunning = true;

        OnSystemMessage?.Invoke($"Connected to {serverIp}:{Port}");
        SendMessage(NetOperation.SYSTEM, $"HELLO {LocalName}");
    }

    public void Tick()
    {
        if (!IsRunning || _sock == null) return;
        try
        {
            while (_sock.Poll(0, SelectMode.SelectRead))
            {
                if (_sock.Available == 0) { OnSystemMessage?.Invoke("Disconnected"); Stop(); return; }
                byte[] tmp = new byte[Math.Min(8192, _sock.Available)];
                int n = _sock.Receive(tmp, tmp.Length, SocketFlags.None);
                if (n <= 0) break;

                _rx.AddRange(new ArraySegment<byte>(tmp, 0, n));

                while (NetCodec.TryDecodeTcp(ref _rx, out var msg))
                {

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
            }
        }
        catch (Exception e) { OnLog?.Invoke(e.Message); }
    }

    public void Send(string text) => SendMessage(NetOperation.CHAT, $"{LocalName}: {text}");

    public void SendMessage(NetOperation op, string payload)
    {
        try
        {
            var bytes = NetCodec.Encode(new NetMsg { op = op, payload = payload }, NetTransport.TCP);
            _sock?.Send(bytes);
        }
        catch (Exception e) { OnLog?.Invoke(e.Message); }
    }

    public void Stop()
    {
        IsRunning = false;
        try { _sock?.Shutdown(SocketShutdown.Both); } catch { }
        try { _sock?.Close(); } catch { }
        _sock = null;
    }
}
