using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string title;
    [TextArea] public string desc;
    public List<StatModEntry> entries = new();
}