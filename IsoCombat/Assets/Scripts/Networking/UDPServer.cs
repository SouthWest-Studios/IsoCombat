using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
    public event Action<NetMsg> OnMessage;

    Socket _sock;
    readonly HashSet<string> _peers = new();

    //Iniciar el servidor
    public void StartServer(string serverName)
    {
        LocalName = serverName;
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _sock.Blocking = false;
        _sock.Bind(new IPEndPoint(IPAddress.Any, Port));
        IsRunning = true;
        OnLog?.Invoke($"UDP server *:{Port}");
    }
    //Iniciar el cliente
    public void StartClient(string serverIp, string clientName) { }
    //Recibir datos UDP y procesar mensajes de red
    public void Tick()
    {
        if (!IsRunning) return;
        byte[] buf = new byte[65535];
        while (true)
        {
            try
            {
                if (!_sock.Poll(0, SelectMode.SelectRead)) break;
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int recv = _sock.ReceiveFrom(buf, ref from);
                if (recv <= 0) break;

                if (!NetCodec.TryDecodeUdp(buf, recv, out var msg)) continue;
                var ep = (IPEndPoint)from;
                _peers.Add(ep.ToString());
                Route(ep, msg);
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); break; }
        }
    }
    // Procesar y enrutar mensajes recibidos
    void Route(IPEndPoint from, NetMsg msg)
    {
       
        Broadcast(msg);
        switch (msg.op)
        {
            case NetOperation.SYSTEM:
                OnSystemMessage?.Invoke("Server: " + msg.payload);
                break;
        }        
        OnMessage?.Invoke(msg);
    }
    // Enviar un mensaje a todos los clientes conectados
    void Broadcast(NetMsg m)
    {
        byte[] data = NetCodec.Encode(m, NetTransport.UDP);
        foreach (var key in _peers)
        {
            var parts = key.Split(':');
            var ep = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            try { _sock.SendTo(data, ep); } catch (Exception e) { OnLog?.Invoke(e.Message); }
        }
    }
    // Enviar un mensaje a un cliente espec¨ªfico
    void SendTo(IPEndPoint ep, NetMsg m)
    {
        try { _sock.SendTo(NetCodec.Encode(m, NetTransport.UDP), ep); }
        catch (Exception e) { OnLog?.Invoke(e.Message); }
    }
    // Enviar mensaje de chat
    public void Send(string text) {
        Debug.Log("No se tendria que usar el chat en UDP -> " + text);
    }


    // Enviar mensaje de red
    public void SendMessage(NetOperation op, string payload)
    {
        Broadcast(new NetMsg { op = op, payload = payload });
        switch(op)
        {
            case NetOperation.SYSTEM:
                OnSystemMessage?.Invoke("Server: " + payload);
                break;
        }
    }
    // Detener el servidor
    public void Stop() { try { _sock?.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); } _sock = null; IsRunning = false; }
}
