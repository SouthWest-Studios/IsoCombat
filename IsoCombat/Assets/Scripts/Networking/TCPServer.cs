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
    readonly List<Socket> _clients = new();
    readonly byte[] _buf = new byte[4096];

    public void StartServer(string serverName)
    {
        LocalName = serverName;
        _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listen.Bind(new IPEndPoint(IPAddress.Any, Port));
        _listen.Listen(16);
        _listen.Blocking = false;
        IsRunning = true;
        SystemMsg($"Server '{LocalName}' listening {Port}");
    }

    public void StartClient(string ip, string clientName) {  }

    public void Tick()
    {
        if (!IsRunning) return;

        // Accept
        if (_listen != null && _listen.Poll(0, SelectMode.SelectRead))
        {
            try
            {
                var s = _listen.Accept();
                SystemMsg($"[SERVER] Client {s.RemoteEndPoint} connected. total={_clients.Count}");
                s.Blocking = false;
                _clients.Add(s);
                SafeSend(s, $"SYSTEM:WELCOME {LocalName}\n");
                SystemMsg($"Client {s.RemoteEndPoint} connected");
            }
            catch (Exception e) { Log(e.Message); }
        }

        // Read
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            try
            {
                if (!c.Poll(0, SelectMode.SelectRead)) continue;
                if (c.Available == 0) { SystemMsg($"Client {c.RemoteEndPoint} disconnected"); c.Close(); _clients.RemoveAt(i); continue; }
                int recv = c.Receive(_buf, Math.Min(_buf.Length, c.Available), SocketFlags.None);
                if (recv <= 0) continue;
                var chunk = System.Text.Encoding.UTF8.GetString(_buf, 0, recv);
                foreach (var line in chunk.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    HandleIncoming(c, line);
                }
            }
            catch (Exception e) { Log(e.Message); }
        }
    }

    void HandleIncoming(Socket from, string line)
    {
        if (line.StartsWith("CHAT:"))
        {
            Broadcast(line + "\n");
            var p = line.Split(':');
            if (p.Length >= 3) OnChatMessage?.Invoke($"{p[1]}: {line.Substring(5 + p[1].Length)}");
        }
        if (line.StartsWith("SYSTEM:"))
        {
            Broadcast(line + "\n");
            OnSystemMessage?.Invoke("Server: " + line.Substring(7));
        }
    }


    void Broadcast(string s)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var c = _clients[i];
            try
            {
                if (c == null) { _clients.RemoveAt(i); continue; }
                c.Send(bytes);
            }
            catch
            {
                try { c.Close(); } catch { }
                _clients.RemoveAt(i);
            }
        }
    }


    void SafeSend(Socket c, string s)
    {
        try { c?.Send(System.Text.Encoding.UTF8.GetBytes(s)); }
        catch (Exception e) { Log(e.Message); }
    }


    public void Stop()
    {
        IsRunning = false;


        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            try { _clients[i]?.Shutdown(SocketShutdown.Both); } catch { }
            try { _clients[i]?.Close(); } catch { }
            _clients.RemoveAt(i);
        }

        try { _listen?.Close(); } catch (Exception e) { Log(e.Message); }
        _listen = null;

        SystemMsg("Server stopped");
    }

    public void Send(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("SYSTEM:")) { Broadcast(line + "\n"); SystemMsg("Server: " + line.Substring(7)); }
            else { Broadcast($"CHAT:{LocalName}:{line}\n"); OnChatMessage?.Invoke($"{LocalName}: {line}"); }
        }
    }

    void Log(string s) => OnLog?.Invoke(s);
    void SystemMsg(string s) => OnSystemMessage?.Invoke(s);
}
