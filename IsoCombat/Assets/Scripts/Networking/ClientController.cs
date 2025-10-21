using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClientController : MonoBehaviour
{
    public Lobby lobby;
    public TMP_InputField ipInput;
    public TextMeshProUGUI header;
    INetwork _net;


    public void OnConnect()
    {
        if (_net != null) { _net.Stop(); _net = null; }

        _net = SessionConfig.Transport == TransportType.TCP ? new TCPClient() : new UDPClient();
        _net.Port = SessionConfig.Port;
        var ip = string.IsNullOrEmpty(ipInput.text) ? "127.0.0.1" : ipInput.text;
        _net.StartClient(ip, SessionConfig.PlayerName);
        lobby.Bind(_net);
        if (header) header.text = $"Client ({SessionConfig.Transport}) – {SessionConfig.PlayerName} -> {ip}:{SessionConfig.Port}";
    }


    void OnDestroy() { _net?.Stop(); }
}