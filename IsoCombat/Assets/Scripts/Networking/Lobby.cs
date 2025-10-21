using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    public TextMeshProUGUI logText;
    public TextMeshProUGUI systemText;
    public TextMeshProUGUI chatText;
    public TMP_InputField chatInput;
    public TMP_InputField serverInput;
    public Button playButton;

    INetwork _net;
    System.Action<string> _externalSystemHandler;

    public void Bind(INetwork net, System.Action<string> onSystem = null)
    {
        _net = net;
        _externalSystemHandler = onSystem;
        _net.OnLog += AppendLog;
        _net.OnSystemMessage += s => { Append(systemText, s); _externalSystemHandler?.Invoke(s); };
        _net.OnChatMessage += s => Append(chatText, s);

        if (playButton) playButton.gameObject.SetActive(_net.IsServer);
    }

    public void OnSendChat()
    {
        if (_net == null) return;
        var t = chatInput.text;
        chatInput.text = string.Empty;
        if (!string.IsNullOrEmpty(t)) _net.Send(t);
    }

    public void OnSendAsServer()
    {
        if (_net == null) return;
        var t = "SYSTEM:" + serverInput.text;
        serverInput.text = string.Empty;
        if (!string.IsNullOrEmpty(t)) _net.Send(t);
    }

    void Update() { _net?.Tick(); }
    void AppendLog(string s) => Append(logText, s);
    void Append(TextMeshProUGUI text, string s) { if (text == null) return; text.text += s + "\n"; }
}
