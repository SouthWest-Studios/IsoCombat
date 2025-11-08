using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Modifier")]
public class StatModifier : ScriptableObject
{
    public List<StatModEntry> entries = new();
}