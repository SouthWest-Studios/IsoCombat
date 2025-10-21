using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;

public class UDPServer : INetwork
{
    public bool IsServer => true;
    public bool IsRunning { get; private set; }
    public string LocalName { get; set; }
    public int Port { get; set; } = 9050;


    public event Action<string> OnLog;
    public event Action<string> OnChatMessage;
    public event Action<string> OnSystemMessage;


    Socket _sock;
    byte[] _buf = new byte[4096];
    readonly HashSet<string> _peers = new HashSet<string>();


    public void StartServer(string serverName)
    {
        LocalName = serverName;
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _sock.Blocking = false;
        _sock.Bind(new IPEndPoint(IPAddress.Any, Port));
        IsRunning = true;
        Log($"UDP server *:{Port}");
    }

    public void StartClient(string serverIp, string clientName)
    { 
    }



    public void Tick()
    {
        if (!IsRunning) return;
        while (true)
        {
            try
            {
                if (!_sock.Poll(0, SelectMode.SelectRead)) break;
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int recv = _sock.ReceiveFrom(_buf, ref from);
                if (recv <= 0) break;
                var text = System.Text.Encoding.UTF8.GetString(_buf, 0, recv);
                HandleIncoming((IPEndPoint)from, text);
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); break; }
        }
    }


    void HandleIncoming(IPEndPoint from, string text)
    {
        _peers.Add(from.ToString());
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith("HELLO:"))
            {
                SystemMsg($"HELLO {line.Substring(6)} from {from}");
                SendTo(from, $"SYSTEM:{LocalName}\n");
            }
            else if (line.StartsWith("CHAT:"))
            {
                Broadcast(line + "\n");
                var p = line.Split(':');
                if (p.Length >= 3) OnChatMessage?.Invoke($"{p[1]}: {line.Substring(5 + p[1].Length)}");
            }
        }
    }


    void Broadcast(string payload)
    {
        var data = System.Text.Encoding.UTF8.GetBytes(payload);
        foreach (var key in _peers)
        {
            var parts = key.Split(':');
            var ep = new IPEndPoint(System.Net.IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            try { _sock.SendTo(data, ep); } catch (Exception e) { OnLog?.Invoke(e.Message); }
        }
    }


    void SendTo(IPEndPoint ep, string payload)
    { try { _sock.SendTo(System.Text.Encoding.UTF8.GetBytes(payload), ep); } catch (Exception e) { OnLog?.Invoke(e.Message); } }


    public void Send(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("SYSTEM:"))
            {
                Broadcast(line + "\n");                  // <- enviar SYSTEM a clientes
                SystemMsg("Server: " + line.Substring(7)); // eco local en panel sistema
            }
            else
            {
                var payload = $"CHAT:{LocalName}:{line}\n";
                Broadcast(payload);                        // chat a clientes
                OnChatMessage?.Invoke($"{LocalName}: {line}"); // eco local en chat
            }
        }
    }
    public void Stop() { try { _sock?.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); } _sock = null; IsRunning = false; }
    void Log(string s) => OnLog?.Invoke(s);
    void SystemMsg(string s) => OnSystemMessage?.Invoke(s);
}
