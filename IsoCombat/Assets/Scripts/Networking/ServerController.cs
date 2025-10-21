using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerController : MonoBehaviour
{
    public Lobby lobby;
    public TextMeshProUGUI header;
    INetwork _net;


    void Start()
    {
        _net = SessionConfig.Transport == TransportType.TCP ? new TCPServer() : new UDPServer();
        _net.Port = SessionConfig.Port;
        _net.StartServer(SessionConfig.PlayerName);
        lobby.Bind(_net);
        if (header) header.text = $"Server ({SessionConfig.Transport}) – {SessionConfig.PlayerName}:{SessionConfig.Port}";
    }


    void OnDestroy() { _net?.Stop(); }
}
