using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string title;
    public string desc;
    public List<StatModifier> modifiers = new();
}