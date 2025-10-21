using System;
using UnityEngine;

public interface INetwork
{
    bool IsServer { get; }
    bool IsRunning { get; }
    string LocalName { get; set; }
    int Port { get; set; }

    void StartServer(string serverName);
    void StartClient(string serverIp, string clientName);

    void Stop();
    void Send(string text);

    event Action<string> OnLog;
    event Action<string> OnChatMessage;
    event Action<string> OnSystemMessage;
    void Tick();
}

