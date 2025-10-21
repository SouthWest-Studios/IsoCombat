using TMPro;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    public Lobby lobby;              // asignar
    public TextMeshProUGUI header;   // opcional
    public GameObject playButton;    // botón "Jugar" (solo servidor)

    INetwork _net;

    void Start()
    {
        _net = SessionConfig.Transport == TransportType.TCP
            ? (INetwork)(SessionConfig.IsHost ? new TCPServer() : new TCPClient())
            : (INetwork)(SessionConfig.IsHost ? new UDPServer() : new UDPClient());

        _net.Port = SessionConfig.Port;

        if (SessionConfig.IsHost)
            _net.StartServer(SessionConfig.PlayerName);
        else
            _net.StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        lobby.Bind(_net, OnSystemMessage); // ver sobrecarga abajo

        if (header)
        {
            header.text = SessionConfig.IsHost
                ? $"Server ({SessionConfig.Transport}) – {SessionConfig.PlayerName}:{SessionConfig.Port}"
                : $"Client ({SessionConfig.Transport}) – {SessionConfig.PlayerName} -> {SessionConfig.ServerIp}:{SessionConfig.Port}";
        }

        if (playButton) playButton.SetActive(_net.IsServer);
    }

    public void OnClickPlay()
    {
        // avisa a todos
        _net.Send("SYSTEM:__PLAY__");
        // y entra localmente
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
    }

    void OnDestroy() { _net?.Stop(); }

    // recibe SYSTEM del net y detecta PLAY
    void OnSystemMessage(string msg)
    {
        // msg suele venir como "Server: <texto>" en clientes
        if (msg.Contains("__PLAY__"))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
        }
    }
}
