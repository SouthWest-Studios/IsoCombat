using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public TMP_InputField nameInput;
    public TMP_InputField portInput;

    public GameObject joinPopup;
    public TMP_InputField ipJoinInput;


    void Start()
    {
        nameInput.text = SessionConfig.PlayerName;
        portInput.text = SessionConfig.Port.ToString();
        if (joinPopup) joinPopup.SetActive(false);
    }

    //Crear una sala
    public void OnCreateRoom()
    {
        ApplySession();
        SessionConfig.IsHost = true;
        CircleTransition.instance.CloseBlackScreen("Lobby");
    }

    //Unirse a la sala
    public void OnJoinRoom()
    {
        ApplySession();
        if (joinPopup) { joinPopup.SetActive(true); ipJoinInput.text = "127.0.0.1"; }
        //SceneManager.LoadScene("Client");
    }

    //Config¨²rese como cliente
    //Configure la IP del servidor 
    //Desactive la pantalla negra en el lobby
    public void OnJoinPopupConnect()
    {
        SessionConfig.IsHost = false;
        SessionConfig.ServerIp = string.IsNullOrEmpty(ipJoinInput.text) ? "127.0.0.1" : ipJoinInput.text;
        CircleTransition.instance.CloseBlackScreen("Lobby");
    }
    //Ocultar ventana emergente
    public void OnJoinPopupCancel()
    {
        if (joinPopup) joinPopup.SetActive(false);
    }

    //Establecer el nombre del jugador y el puerto de red
    void ApplySession()
    {
        SessionConfig.PlayerName = string.IsNullOrEmpty(nameInput.text) ? "Player" : nameInput.text;
        SessionConfig.Port = int.Parse(portInput.text);
    }
}
