using System.Collections.Generic;
using System;
using UnityEngine;

public class UpgradesState : MonoBehaviour
{
    public static UpgradesState I;
    void Awake() { if (I == null) { I = this; DontDestroyOnLoad(gameObject); } else Destroy(gameObject); }

    // por jugador
    public readonly Dictionary<string, List<StatModEntry>> byPlayer = new();

    public IEnumerable<StatModEntry> GetFor(string playerId) =>
        byPlayer.TryGetValue(playerId, out var list) ? list : Array.Empty<StatModEntry>();

    public void AddUpgrade(string playerId, Upgrade u)
    {
        if (!byPlayer.TryGetValue(playerId, out var list)) { list = new(); byPlayer[playerId] = list; }
        foreach (var mod in u.modifiers) list.AddRange(mod.entries);
    }

    public void ResetRound() { /* si quieres limpiar por ronda */ }
    public void ResetMatch() { byPlayer.Clear(); }
}