using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public TMP_Dropdown transportDropdown;
    public TMP_InputField nameInput;
    public TMP_InputField portInput;


    void Start()
    {
        nameInput.text = SessionConfig.PlayerName;
        portInput.text = SessionConfig.Port.ToString();
        transportDropdown.value = (int)SessionConfig.Transport;
    }


    public void OnCreateRoom()
    {
        ApplySession();
        SceneManager.LoadScene("Server");
    }


    public void OnJoinRoom()
    {
        ApplySession();
        SceneManager.LoadScene("Client");
    }


    void ApplySession()
    {
        SessionConfig.Transport = (TransportType)transportDropdown.value;
        SessionConfig.PlayerName = string.IsNullOrEmpty(nameInput.text) ? "Player" : nameInput.text;
        SessionConfig.Port = int.Parse(portInput.text);

    }
}
