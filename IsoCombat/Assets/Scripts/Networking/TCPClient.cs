using System.Net.Sockets;
using System.Net;
using System;
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
    byte[] _buf = new byte[4096];

    public void StartServer(string serverName)
    { 
    }

    public void StartClient(string serverIp, string clientName)
    {
        LocalName = clientName;
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _sock.Blocking = true;
        try
        {
            _sock.Connect(new IPEndPoint(IPAddress.Parse(serverIp), Port));
            _sock.Blocking = false;
            IsRunning = true;
            Log($"Connected TCP {serverIp}:{Port}");
            SendRaw($"HELLO:{LocalName}\n");
        }
        catch (Exception e)
        {
            Log("Connect failed: " + e.Message);
            Stop();
        }
    }


    public void Tick()
    {
        if (!IsRunning || _sock == null) return;
        try
        {
            if (!_sock.Poll(0, SelectMode.SelectRead)) return;
            int available = _sock.Available;
            if (available == 0) { SystemMsg("Disconnected"); Stop(); return; }
            int recv = _sock.Receive(_buf, Math.Min(available, _buf.Length), SocketFlags.None);
            if (recv <= 0) return;
            var text = System.Text.Encoding.UTF8.GetString(_buf, 0, recv);
            foreach (var line in text.Split('\n'))
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
        catch (Exception e) { OnLog?.Invoke(e.Message); }
    }


    public void Send(string text)
    {
        SendRaw($"CHAT:{LocalName}:{text}\n");


        foreach (var line in text.Split('\n'))
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

    //public void Send(string text) => SendRaw($"CHAT:{LocalName}:{text}\n");
    void SendRaw(string s) { try { _sock?.Send(System.Text.Encoding.UTF8.GetBytes(s)); } catch (Exception e) { OnLog?.Invoke(e.Message); } }
    public void Stop() { try { _sock?.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); } _sock = null; IsRunning = false; }
    void Log(string s) => OnLog?.Invoke(s);
    void SystemMsg(string s) => OnSystemMessage?.Invoke(s);
}