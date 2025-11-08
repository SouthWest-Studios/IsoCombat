using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameNet : MonoBehaviour
{
    public PlayerRankUI[] rankUI;
    INetwork net;

    readonly HashSet<string> ready = new();

    void Start()
    {
        INetwork t = SessionConfig.IsHost ? (INetwork)new TCPServer() : new TCPClient();
        t.Port = SessionConfig.Port;
        if (SessionConfig.IsHost) t.StartServer(SessionConfig.PlayerName);
        else t.StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        NetRuntime.Attach(t); net = t;
        net.OnMessage += OnMsg;

        Render(LocalSnapshot());

        if (SessionConfig.IsHost)
        {
            ready.Add(SessionConfig.ClientId);   
            BroadcastRank();                     
        }
        else
        {
            net.SendMessage(NetOperation.MG_READY, SessionConfig.ClientId);
        }
    }

    void Update() { net?.Tick(); }
    void OnDestroy() { if (net != null) net.OnMessage -= OnMsg; }

    Dictionary<string, int> LocalSnapshot() => new(NetRuntime.winners);

    void Render(Dictionary<string, int> wins)
    {
        var ordened = new List<KeyValuePair<string, int>>(wins);
        ordened.Sort((a, b) => b.Value.CompareTo(a.Value));
        int count = Mathf.Min(ordened.Count, rankUI.Length);
        for (int i = 0; i < count; i++)
        {
            var e = ordened[i];
            var parts = e.Key.Split('_');
            string display = parts.Length > 1 ? parts[1] : e.Key;
            rankUI[i].text.text = $"{i + 1}. {display}";
            rankUI[i].rank_bar.fillAmount = Mathf.Clamp01(e.Value / 3f);
        }
        for (int i = count; i < rankUI.Length; i++) { rankUI[i].text.text = ""; rankUI[i].rank_bar.fillAmount = 0f; }
    }

    void OnMsg(NetMsg m)
    {
        switch (m.op)
        {
            case NetOperation.SHOW_RANK:
                var rp = JsonUtility.FromJson<RankPayload>(m.payload);
                if (rp.entries == null || rp.entries.Length == 0) return;
                NetRuntime.winners.Clear();
                foreach (var e in rp.entries) NetRuntime.winners[e.name] = e.wins;
                Render(LocalSnapshot());
                break;

            case NetOperation.MG_READY:
                if (!SessionConfig.IsHost) break;
                if (ready.Add(m.payload)) BroadcastRank();
                break;

            case NetOperation.BACK_TO_LOBBY:
                net.Stop();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
                break;
        }
    }

    public void onBackToLobby()
    {
        net.SendMessage(NetOperation.BACK_TO_LOBBY, "");
        net.Stop();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }


    void BroadcastRank()
    {
        if (!SessionConfig.IsHost) return;
        var list = new List<RankEntry>();
        foreach (var kv in NetRuntime.winners) list.Add(new RankEntry { name = kv.Key, wins = kv.Value });
        var payload = JsonUtility.ToJson(new RankPayload { entries = list.ToArray() });
        net.SendMessage(NetOperation.SHOW_RANK, payload);
    }
}

