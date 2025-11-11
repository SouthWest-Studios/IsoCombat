using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[Serializable] public struct RankEntry{public string name; public int wins; }
[Serializable] public struct RankPayload { public RankEntry[] entries; }
[Serializable] public struct PlayerRankUI{ public TextMeshProUGUI text; public Image rank_bar; }

[Serializable] public struct UpgradePick { public string playerId; public StatModEntry[] mods; }
[Serializable] public struct PlayerMods { public string playerId; public StatModEntry[] mods; }
[Serializable] public struct StatsSync { public PlayerMods[] players; }

public class MidGameNet : MonoBehaviour
{
    public PlayerRankUI[] rankUI;
    INetwork net;

    readonly HashSet<string> ready = new();
    readonly HashSet<string> picked = new();

    public static MidGameNet I;

    private void Awake()
    {
        MidGameNet.I = this;
    }

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
            if (!SessionConfig.IsSpectator) net.SendMessage(NetOperation.MG_READY, SessionConfig.ClientId);
        }
    }

    void Update() { net?.Tick(); }
    void OnDestroy() { if (net != null) net.OnMessage -= OnMsg; }

    Dictionary<string, int> LocalSnapshot() => new(NetRuntime.winners);

    void Render(Dictionary<string, int> wins)
    {
        var ordened = new List<KeyValuePair<string, int>>(wins);
        ordened.Sort((a, b) => {
            return string.Compare(a.Key, b.Key, StringComparison.Ordinal);
        });
        int count = Mathf.Min(ordened.Count, rankUI.Length);
        for (int i = 0; i < count; i++)
        {
            var e = ordened[i];
            var parts = e.Key.Split('_');
            string display = parts.Length > 1 ? parts[1] : e.Key;
            rankUI[i].text.text = $"{display}";
            rankUI[i].rank_bar.fillAmount = Mathf.Clamp01(e.Value / 3f);
        }
        for (int i = count; i < rankUI.Length; i++) { rankUI[i].text.text = ""; rankUI[i].rank_bar.fillAmount = 0f; }
    }

    public void SendUpgradePicked(UpgradePick p)
    {
        if (SessionConfig.IsSpectator) return;
        
        string json = JsonUtility.ToJson(p);
        net.SendMessage(NetOperation.UPGRADE_PICKED, json);
        if (SessionConfig.IsHost)
        {
            HostApplyPick(p);      
            TryAdvanceRound();
        }
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
                TryAdvanceRound();
                break;

            case NetOperation.UPGRADE_PICKED:
                if (!SessionConfig.IsHost) break;
                var pick = JsonUtility.FromJson<UpgradePick>(m.payload);
                HostApplyPick(pick);
                TryAdvanceRound();
                break;

            case NetOperation.STATS_SYNC:
                var sync = JsonUtility.FromJson<StatsSync>(m.payload);
                ApplyStatsSync(sync);
                break;

            case NetOperation.PLAY:
                UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
                break;
        }
    }

    void HostApplyPick(UpgradePick p)
    {
        if (picked.Contains(p.playerId)) return;
        UpgradesState.I.AddMods(p.playerId, p.mods);
        picked.Add(p.playerId);
    }

    void TryAdvanceRound()
    {
        if (!SessionConfig.IsHost) return;
        if (ready.Count == 0 || picked.Count < ready.Count) return;

        // snapshot mods por jugador
        var players = new List<PlayerMods>();
        foreach (var kv in UpgradesState.I.byPlayer)
            players.Add(new PlayerMods { playerId = kv.Key, mods = kv.Value.ToArray() });

        var sync = new StatsSync { players = players.ToArray() };
        var json = JsonUtility.ToJson(sync);

        net.SendMessage(NetOperation.STATS_SYNC, json);
        net.SendMessage(NetOperation.PLAY, ""); 
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");

        ready.Clear(); picked.Clear();
    }

    void ApplyStatsSync(StatsSync s)
    {
        UpgradesState.I.ClearAll();
        foreach (var pm in s.players) UpgradesState.I.AddMods(pm.playerId, pm.mods);
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

