using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine.UI;
using UnityEngine;

public class TCPClient : INetwork
{
    public bool IsServer => false;
    public bool IsRunning { get; private set; }
    public string LocalName { get; set; }
    public int Port { get; set; } = 9050;

    public event Action<string> OnLog;
    public event Action<string> OnChatMessage;
    public event Action<string> OnSystemMessage;

    Socket _sock;

    public void StartServer(string n) { /* no-op en cliente */ }

    public void StartClient(string serverIp, string clientName)
    {
        LocalName = clientName;
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _sock.Connect(IPAddress.Parse(serverIp), Port);
        _sock.Blocking = false;
        IsRunning = true;
        SystemMsg($"Connected to {serverIp}:{Port}");


        Send($"SYSTEM:HELLO {LocalName}");
        SystemMsg($"[CLIENT] Connected {serverIp}:{Port} as {LocalName} id={SessionConfig.ClientId}");

    }

    public void Tick()
    {
        if (!IsRunning || _sock == null) return;
        try
        {
            while (_sock.Poll(0, SelectMode.SelectRead))
            {
                if (_sock.Available == 0) { SystemMsg("Disconnected"); Stop(); return; }
                var buf = new byte[Math.Min(4096, _sock.Available)];
                int n = _sock.Receive(buf, buf.Length, SocketFlags.None);
                if (n <= 0) break;
                var chunk = System.Text.Encoding.UTF8.GetString(buf, 0, n);
                foreach (var line in chunk.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("SYSTEM:")) SystemMsg("Server: " + line.Substring(7));
                    else if (line.StartsWith("CHAT:"))
                    {
                        var p = line.Split(':');
                        if (p.Length >= 3) OnChatMessage?.Invoke($"{p[1]}: {line.Substring(5 + p[1].Length)}");
                    }
                }
            }
        }
        catch (Exception e) { Log(e.Message); }
    }

    public void Send(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            var outLine = line.StartsWith("SYSTEM:") ? line : $"CHAT:{LocalName}:{line}";
            SendRaw(outLine + "\n");
            if (!line.StartsWith("SYSTEM:")) OnChatMessage?.Invoke($"{LocalName}: {line}");
        }
    }

    void SendRaw(string s)
    {
        try
        {
            if (_sock == null) return;
            var bytes = System.Text.Encoding.UTF8.GetBytes(s);
            _sock.Send(bytes);
        }
        catch (Exception e) { Log(e.Message); }
    }

    public void Stop()
    {
        IsRunning = false;
        try { _sock?.Shutdown(SocketShutdown.Both); } catch { }
        try { _sock?.Close(); } catch (Exception e) { Log(e.Message); }
        _sock = null;
    }

    void Log(string s) => OnLog?.Invoke(s);
    void SystemMsg(string s) => OnSystemMessage?.Invoke(s);
}
