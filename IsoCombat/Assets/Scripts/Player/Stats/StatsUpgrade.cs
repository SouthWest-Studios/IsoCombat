using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string title;
    public Sprite icon;
    [Range(1, 5)]
    public int starsRarity;
    public List<StatModEntry> entries = new();

}