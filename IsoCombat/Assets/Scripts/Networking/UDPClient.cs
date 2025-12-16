using System;
using System.Net;
using System.Net.Sockets;
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
    public event Action<NetMsg> OnMessage;

    Socket _sock;
    IPEndPoint _server;

    //Iniciar el servidor
    public void StartServer(string serverName) { }

    //Iniciar el cliente
    public void StartClient(string serverIp, string clientName)
    {
        LocalName = clientName;
        _server = new IPEndPoint(IPAddress.Parse(serverIp), Port);
        _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _sock.Blocking = false;
        _sock.Bind(new IPEndPoint(IPAddress.Any, 0));
        IsRunning = true;

    }
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


                switch (msg.op)
                {
                    case NetOperation.SYSTEM:
                        OnSystemMessage?.Invoke("Server: " + msg.payload);
                        break;
                }
                OnMessage?.Invoke(msg);
            }
            catch (Exception e) { OnLog?.Invoke(e.Message); break; }
        }
    }
    // Enviar mensaje de chat
    public void Send(string text)
    {
        Debug.Log("No se tendria que usar el chat en UDP -> " + text);
    }
    // Enviar mensaje de red
    public void SendMessage(NetOperation op, string payload)
    {
        var bytes = NetCodec.Encode(new NetMsg { op = op, payload = payload }, NetTransport.UDP);
        try { _sock.SendTo(bytes, _server); } catch (Exception e) { OnLog?.Invoke(e.Message); }
    }
    // Detener la conexi¨®n
    public void Stop() { try { _sock?.Close(); } catch (Exception e) { OnLog?.Invoke(e.Message); } _sock = null; IsRunning = false; }
}
