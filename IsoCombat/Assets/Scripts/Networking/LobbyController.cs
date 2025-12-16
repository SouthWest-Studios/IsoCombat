using TMPro;
using UnityEngine;

public class LobbyController : MonoBehaviour
{
    public Lobby lobby;              
    public TextMeshProUGUI header;   
    public GameObject playButton;    

    INetwork _net;

    void Start()
    {
        _net = SessionConfig.IsHost ? (INetwork)new TCPServer()
                                    : (INetwork)new TCPClient();
        _net.Port = SessionConfig.Port;

        if (SessionConfig.IsHost) _net.StartServer(SessionConfig.PlayerName);
        else _net.StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        NetRuntime.Attach(_net);
        lobby.Bind(_net, OnSystemMessage);

        if (header)
            header.text = SessionConfig.IsHost
                ? $"Server (TCP) ?{SessionConfig.PlayerName}:{SessionConfig.Port}"
                : $"Client (TCP) ?{SessionConfig.PlayerName} -> {SessionConfig.ServerIp}:{SessionConfig.Port}";

        if (playButton) playButton.SetActive(_net.IsServer);
    }
    //Cuando se presiona el bot¨®n de Play
    public void OnClickPlay()
    {
        _net.SendMessage(NetOperation.SYSTEM, "__PLAY__");
        UpgradesState.I.ClearAll();
        NetRuntime.ResetWinners();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");

    }

    //void OnDestroy() { _net?.Stop(); }

    //Cuando se inicia el juego, borra el estado y carga la escena del juego
    void OnSystemMessage(string msg)
    {
        if (msg.Contains("__PLAY__"))
        {
            UpgradesState.I.ClearAll();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
        }
    }
}
