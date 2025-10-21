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


    public void OnCreateRoom()
    {
        ApplySession();
        SessionConfig.IsHost = true;
        SceneManager.LoadScene("Lobby");
    }


    public void OnJoinRoom()
    {
        ApplySession();
        if (joinPopup) { joinPopup.SetActive(true); ipJoinInput.text = "127.0.0.1"; }
        //SceneManager.LoadScene("Client");
    }

    public void OnJoinPopupConnect()
    {
        SessionConfig.IsHost = false;
        SessionConfig.ServerIp = string.IsNullOrEmpty(ipJoinInput.text) ? "127.0.0.1" : ipJoinInput.text;
        SceneManager.LoadScene("Lobby");
    }

    public void OnJoinPopupCancel()
    {
        if (joinPopup) joinPopup.SetActive(false);
    }


    void ApplySession()
    {
        SessionConfig.PlayerName = string.IsNullOrEmpty(nameInput.text) ? "Player" : nameInput.text;
        SessionConfig.Port = int.Parse(portInput.text);
    }
}
