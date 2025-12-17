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

    //Create a room
    public void OnCreateRoom()
    {
        ApplySession();
        SessionConfig.IsHost = true;
        CircleTransition.instance.CloseBlackScreen("Lobby");
    }

    //Join the room
    public void OnJoinRoom()
    {
        ApplySession();
        if (joinPopup) { joinPopup.SetActive(true); ipJoinInput.text = "127.0.0.1"; }
        //SceneManager.LoadScene("Client");
    }

    //Configure as a client
    //Configure the server IP address
    //Disable the black screen in the lobby
    public void OnJoinPopupConnect()
    {
        SessionConfig.IsHost = false;
        SessionConfig.ServerIp = string.IsNullOrEmpty(ipJoinInput.text) ? "127.0.0.1" : ipJoinInput.text;
        CircleTransition.instance.CloseBlackScreen("Lobby");
    }
    //Hide pop-up window
    public void OnJoinPopupCancel()
    {
        if (joinPopup) joinPopup.SetActive(false);
    }

    //Set player name and network port
    void ApplySession()
    {
        SessionConfig.PlayerName = string.IsNullOrEmpty(nameInput.text) ? "Player" : nameInput.text;
        SessionConfig.Port = int.Parse(portInput.text);
    }
}
