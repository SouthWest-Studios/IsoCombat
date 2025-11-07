using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public struct RankEntry
{
    public string name;
    public int wins;
}


[Serializable]
public struct RankPayload {
    public RankEntry[] entries;
}


public class MidGameNet : MonoBehaviour
{
    public TextMeshProUGUI text;
    INetwork net;

    private int playerLoadedCounter = 1; //Server es +1

    void Start()
    {
        INetwork t = SessionConfig.IsHost ? (INetwork)new TCPServer()
                                          : (INetwork)new TCPClient();
        t.Port = SessionConfig.Port;
        if (SessionConfig.IsHost) t.StartServer(SessionConfig.PlayerName);
        else t.StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        NetRuntime.Attach(t);
        net = t;
        net.OnMessage += OnMsg;
        net.OnSystemMessage += s => Debug.Log(s);

        Render(LocalSnapshot());

        if (!SessionConfig.IsHost) net.SendMessage(NetOperation.RANK_READY, "");
    }

    void Update() { 
        net?.Tick(); 
    }

    void OnDestroy() { if (net != null) net.OnMessage -= OnMsg; }

    Dictionary<string, int> LocalSnapshot() => new Dictionary<string, int>(NetRuntime.winners);

    void Render(Dictionary<string, int> wins)
    {
        // Render simple: ordena por victorias desc.
        var arr = new List<KeyValuePair<string, int>>(wins);
        arr.Sort((a, b) => b.Value.CompareTo(a.Value));

        var lines = new List<string> { "RANKING" };
        int pos = 1;
        foreach (var kv in arr) lines.Add($"{pos++}. {kv.Key}  - {kv.Value}");
        if (text != null) text.text = string.Join("\n", lines);
        else Debug.Log(string.Join("\n", lines));
    }

    void OnMsg(NetMsg m)
    {
        if (m.op == NetOperation.SHOW_RANK)
        {
            var p = JsonUtility.FromJson<RankPayload>(m.payload);
            if (p.entries == null || p.entries.Length == 0) return;

            NetRuntime.winners.Clear();
            foreach (var e in p.entries) NetRuntime.winners[e.name] = e.wins;
            Render(LocalSnapshot());
            return;
        }
        else if (m.op == NetOperation.RANK_READY)
        {
            if (!SessionConfig.IsHost) return;
            BroadcastRank();
            return;
        }
        else if(m.op == NetOperation.BACK_TO_LOBBY)
    {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }
    }

    public void OnBackToLobbyButton()
    {
        net.SendMessage(NetOperation.BACK_TO_LOBBY, "");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }


    void BroadcastRank()
    {
        if (!SessionConfig.IsHost) return;
        var list = new List<RankEntry>();
        foreach (var kv in NetRuntime.winners)
        {
            list.Add(new RankEntry { name = kv.Key, wins = kv.Value });
        }
        var payload = JsonUtility.ToJson(new RankPayload { entries = list.ToArray() });
        net.SendMessage(NetOperation.SHOW_RANK, payload);
    }
}
