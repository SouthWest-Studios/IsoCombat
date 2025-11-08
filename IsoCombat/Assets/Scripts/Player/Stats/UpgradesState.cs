using System.Collections.Generic;
using System;
using UnityEngine;

public class UpgradesState : MonoBehaviour
{
    public static UpgradesState I;
    void Awake() { if (I == null) { I = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

    public readonly Dictionary<string, List<StatModEntry>> byPlayer = new();

    public IEnumerable<StatModEntry> GetFor(string playerId) =>
        byPlayer.TryGetValue(playerId, out var list) ? list : Array.Empty<StatModEntry>();

    public void AddUpgrade(string playerId, Upgrade u)
    {
        if (!byPlayer.TryGetValue(playerId, out var list)) { list = new(); byPlayer[playerId] = list; }
        list.AddRange(u.entries);
    }

    public void AddMods(string playerId, IEnumerable<StatModEntry> mods)
    {
        if (!byPlayer.TryGetValue(playerId, out var list)) { list = new(); byPlayer[playerId] = list; }
        list.AddRange(mods);
    }
    public void ClearAll() => byPlayer.Clear();

    public void ResetRound() { }
    public void ResetMatch() { byPlayer.Clear(); }
}