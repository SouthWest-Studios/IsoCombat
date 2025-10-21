using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;

public class TCPServer : INetwork
{
    public bool IsServer => true;
    public bool IsRunning { get; private set; }
    public string LocalName { get; set; }
    public int Port { get; set; } = 9050;


    public event Action<string> OnLog;
    public event Action<string> OnChatMessage;
    public event Action<string> OnSystemMessage;


    Socket _listen;
    readonly List<Socket> _clients = new List<Socket>();
    byte[] _buf = new byte[4096];


    public void StartServer(string serverName)
    {
        LocalName = serverName;
        _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listen.Blocking = false;
        _listen.Bind(new IPEndPoint(IPAddress.Any, Port));
        _listen.Listen(16);
        IsRunning = true;
        Log($"TCP server *:{Port}");
    }

    public void StartClient(string serverIp, string clientName)
    {

    }


    public void Tick()
    {
        if (!IsRunning) return;

        if (_listen != null && _listen.Poll(0, SelectMode.SelectRead))
        {
            try
            {
                var c = _listen.Accept();
                c.Blocking = false;
                _clients.Add(c);
                SystemMsg($"Client {c.RemoteEndPoint} connected");
                SafeSend(c, $"SYSTEM:{LocalName}\n");
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); }
        }

        for (int i = _clients.Count - 1; i >= 0; --i)
        {
            var c = _clients[i];
            try
            {
                if (!c.Poll(0, SelectMode.SelectRead)) continue;
                int available = c.Available;
                if (available == 0)
                {
                    SystemMsg($"Client {c.RemoteEndPoint} disconnected");
                    c.Close();
                    _clients.RemoveAt(i);
                    continue;
                }
                int recv = c.Receive(_buf, Math.Min(available, _buf.Length), SocketFlags.None);
                if (recv > 0) HandleIncoming(c, System.Text.Encoding.UTF8.GetString(_buf, 0, recv));
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); }
        }
    }


    void HandleIncoming(Socket c, string chunk)
    {
        var lines = chunk.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("HELLO:")) SystemMsg($"HELLO {line.Substring(6)}");
            else if (line.StartsWith("CHAT:"))
            {
                Broadcast(line + "\n");
                var p = line.Split(':');
                if (p.Length >= 3) Chat($"{p[1]}: {line.Substring(5 + p[1].Length)}");
            }
        }
    }


    void Broadcast(string payload)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
        for (int i = _clients.Count - 1; i >= 0; --i)
        {
            try { _clients[i].Send(bytes); }
            catch (Exception e) { _clients.RemoveAt(i); OnLog?.Invoke(e.Message); }
        }
    }


    void SafeSend(Socket c, string payload) { try { c.Send(System.Text.Encoding.UTF8.GetBytes(payload)); } catch (Exception e) { OnLog?.Invoke(e.Message); } }


    public void Send(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("SYSTEM:"))
            {
                Broadcast(line + "\n");
                SystemMsg("Server: " + line.Substring(7));
            }
            else
            {
                var payload = $"CHAT:{LocalName}:{line}\n";
                Broadcast(payload);
                OnChatMessage?.Invoke($"{LocalName}: {line}");
            }
        }
    }

    public void Stop()
    {
        IsRunning = false;
        try { _listen?.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); }
        foreach (var c in _clients) try { c.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); }
        _clients.Clear();
    }


    void Log(string s) => OnLog?.Invoke(s);
    void SystemMsg(string s) => OnSystemMessage?.Invoke(s);
    void Chat(string s) => OnChatMessage?.Invoke(s);
}
