using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public struct StatModEntry { public StatId id; public ModOp op; public float value; }

public class StatsRuntime : MonoBehaviour
{
    public StatsBase baseStats;
    readonly Dictionary<StatId, float> finalValues = new();
    readonly List<StatModEntry> mods = new();

    void Awake() { Recalc(); }

    public void SetModifiers(IEnumerable<StatModEntry> allMods)
    {
        mods.Clear(); mods.AddRange(allMods); Recalc();
    }

    void Recalc()
    {
        finalValues[StatId.MaxHP] = baseStats.Get(StatId.MaxHP);
        finalValues[StatId.MoveSpeed] = baseStats.Get(StatId.MoveSpeed);
        finalValues[StatId.Scale] = baseStats.Get(StatId.Scale);
        finalValues[StatId.Stun] = baseStats.Get(StatId.Stun);
        finalValues[StatId.Regen] = baseStats.Get(StatId.Regen);
        finalValues[StatId.RegenSpeed] = baseStats.Get(StatId.RegenSpeed);
        finalValues[StatId.BulletSpeed] = baseStats.Get(StatId.BulletSpeed);
        finalValues[StatId.BulletRate] = baseStats.Get(StatId.BulletRate);
        finalValues[StatId.InvisSpeed] = baseStats.Get(StatId.InvisSpeed);
        finalValues[StatId.InvisCount] = baseStats.Get(StatId.InvisCount);

        foreach (var m in mods) if (m.op == ModOp.Add) finalValues[m.id] += m.value;
        foreach (var m in mods) if (m.op == ModOp.Mul) finalValues[m.id] *= (1f + m.value);
        foreach (var m in mods) if (m.op == ModOp.Override) finalValues[m.id] = m.value;

        
        finalValues[StatId.Scale] = Mathf.Max(0.2f, finalValues[StatId.Scale]);
        finalValues[StatId.MoveSpeed] = Mathf.Max(0.5f, finalValues[StatId.MoveSpeed]);
    }

    public float Get(StatId id) => finalValues.TryGetValue(id, out var v) ? v : 0f;
}
