using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Lobby : MonoBehaviour
{
    public TextMeshProUGUI logText;
    public TextMeshProUGUI systemText;
    public TextMeshProUGUI chatText;
    public TMP_InputField chatInput;
    public TMP_InputField serverInput;
    public Button playButton;

    public float maxChatHeight = 270f;
    List<string> _chatMessages = new List<string>();

    INetwork _net;
    System.Action<string> _externalSystemHandler;
    
    public void Bind(INetwork net, System.Action<string> onSystem = null)
    {
        _net = net;
        _externalSystemHandler = onSystem;
        _net.OnLog += AppendLog;
        _net.OnSystemMessage += s => { Append(systemText, s); _externalSystemHandler?.Invoke(s); };
        _net.OnChatMessage += AppendChat;

        if (playButton) playButton.gameObject.SetActive(_net.IsServer);
    }
    //Enviar mensaje de chat
    public void OnSendChat()
    {
        if (_net == null) return;
        string t = chatInput.text;
        chatInput.text = string.Empty;
        if (!string.IsNullOrEmpty(t)) _net.Send(t);
    }
    //Enviar mensaje a la red
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
    void AppendChat(string s) { if (chatText == null || string.IsNullOrEmpty(s)) return; _chatMessages.Add(s); RebuildChatText(); }
    //Actualiza el chat y elimina los mensajes antiguos
    void RebuildChatText()
    {
        if (chatText == null) return;
        chatText.text = string.Join("\n", _chatMessages);
        chatText.ForceMeshUpdate();
        float limit = maxChatHeight > 0f ? maxChatHeight : chatText.rectTransform.rect.height;

        while (_chatMessages.Count > 0 && chatText.preferredHeight > limit)
        {
            _chatMessages.RemoveAt(0);
            chatText.text = string.Join("\n", _chatMessages);
            chatText.ForceMeshUpdate();
        }
    }
}

