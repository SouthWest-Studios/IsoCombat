using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;

public class UDPClient : INetwork
{
    public bool IsServer => false;
    public bool IsRunning { get; private set; }
    public string LocalName { get; set; }
    public int Port { get; set; } = 9050;


    public event Action<string> OnLog;
    public event Action<string> OnChatMessage;
    public event Action<string> OnSystemMessage;


    Socket _sock;
    IPEndPoint _server;
    byte[] _buf = new byte[4096];

    public void StartServer(string serverName) { 
    }

    public void StartClient(string serverIp, string clientName)
    {
        LocalName = clientName;
        _server = new IPEndPoint(System.Net.IPAddress.Parse(serverIp), Port);
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _sock.Blocking = false;
        _sock.Bind(new IPEndPoint(System.Net.IPAddress.Any, 0));
        IsRunning = true;
        Log($"UDP to {_server}");
        SendRaw($"HELLO:{LocalName}\n");
    }


    public void Tick()
    {
        if (!IsRunning) return;
        while (true)
        {
            try
            {
                if (!_sock.Poll(0, SelectMode.SelectRead)) break;
                EndPoint from = new IPEndPoint(System.Net.IPAddress.Any, 0);
                int recv = _sock.ReceiveFrom(_buf, ref from);
                if (recv <= 0) break;
                var text = System.Text.Encoding.UTF8.GetString(_buf, 0, recv);
                foreach (var line in text.Split('\n'))
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith("SYSTEM:")) OnSystemMessage?.Invoke("Server: " + line.Substring(7));
                    else if (line.StartsWith("CHAT:"))
                    {
                        var p = line.Split(':');
                        if (p.Length >= 3) OnChatMessage?.Invoke($"{p[1]}: {line.Substring(5 + p[1].Length)}");
                    }
                }
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); break; }
        }
    }

    public void Send(string text)
    {
        SendRaw($"CHAT:{LocalName}:{text}\n");


        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("SYSTEM:")) OnSystemMessage?.Invoke("Server: " + line.Substring(7));
            else if (line.StartsWith("CHAT:"))
            {
                var p = line.Split(':');
                if (p.Length >= 3) OnChatMessage?.Invoke($"{p[1]}: {line.Substring(5 + p[1].Length)}");
            }
        }
    }

    // public void Send(string text) => SendRaw($"CHAT:{LocalName}:{text}\n");
    void SendRaw(string s) { try { _sock.SendTo(System.Text.Encoding.UTF8.GetBytes(s), _server); } catch (Exception e) { OnLog?.Invoke(e.Message); } }
    public void Stop() { try { _sock?.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); } _sock = null; IsRunning = false; }
    void Log(string s) => OnLog?.Invoke(s);
}
