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


[Serializable]
public struct PlayerRankUI
{
    public TextMeshProUGUI text;
    public Image rank_bar;
}

public class MidGameNet : MonoBehaviour
{
    //public TextMeshProUGUI text;
    public PlayerRankUI[] rankUI;


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
        var ordened = new List<KeyValuePair<string, int>>(wins);
        ordened.Sort((a, b) => b.Key.CompareTo(a.Key));


        int count = Mathf.Min(ordened.Count, rankUI.Length);
        for (int i = 0; i < count; i++)
        {
            var e = ordened[i];
            rankUI[i].text.text = $"{i + 1}. {e.Key.Split("_")[1]}";
            rankUI[i].rank_bar.fillAmount = Mathf.Clamp01(e.Value / 3f);
        }

        for (int i = count; i < rankUI.Length; i++)
        {
            rankUI[i].text.text = "";
            rankUI[i].rank_bar.fillAmount = 0f;
        }
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
        else if(m.op == NetOperation.PLAY)
    {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
        }
    }

    public void OnContinueMatch()
    {
        net.SendMessage(NetOperation.PLAY, "");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
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
