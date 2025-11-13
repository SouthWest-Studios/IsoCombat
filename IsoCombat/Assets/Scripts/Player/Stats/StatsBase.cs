using UnityEngine;

public enum StatId { MaxHP, MoveSpeed, Scale, Stun, Regen, RegenSpeed }
public enum ModOp { Add, Mul, Override }

[CreateAssetMenu(menuName = "Stats/Base")]
public class StatsBase : ScriptableObject
{
    public float maxHP = 3f;
    public float moveSpeed = 5f;
    public float scale = 1f;
    public float stun = 0.5f;
    public float regen = 0f;
    public float regenSpeed = 3f;

    public float Get(StatId id) => id switch
    {
        StatId.MaxHP => maxHP,
        StatId.MoveSpeed => moveSpeed,
        StatId.Scale => scale,
        StatId.Stun => stun,
        StatId.Regen => regen,
        StatId.RegenSpeed => regenSpeed,
        _ => 0f
    };
}