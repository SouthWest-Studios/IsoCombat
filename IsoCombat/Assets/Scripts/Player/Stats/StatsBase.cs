using UnityEngine;

public enum StatId { MaxHP, MoveSpeed, Scale }
public enum ModOp { Add, Mul, Override }

[CreateAssetMenu(menuName = "Stats/Base")]
public class StatsBase : ScriptableObject
{
    public float maxHP = 3f;
    public float moveSpeed = 5f;
    public float scale = 1f;
    public float Get(StatId id) => id switch
    {
        StatId.MaxHP => maxHP,
        StatId.MoveSpeed => moveSpeed,
        StatId.Scale => scale,
        _ => 0f
    };
}