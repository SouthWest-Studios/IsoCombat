using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MidGameUI : MonoBehaviour
{
    public Upgrade[] pool;      // todas las upgrades posibles
    public Button[] choices;    // 3 botones en la UI
    Upgrade[] offered;

    void Start()
    {
        // elige 3 distintas
        offered = pool.OrderBy(_ => Random.value).Take(3).ToArray();
        for (int i = 0; i < choices.Length; i++)
        {
            int idx = i;
            choices[i].GetComponentInChildren<TMPro.TextMeshProUGUI>().text = offered[i].title;
            choices[i].onClick.AddListener(() => Pick(offered[idx]));
        }
    }

    void Pick(Upgrade u)
    {
        // guarda la mejora para este jugador
        UpgradesState.I.AddUpgrade(SessionConfig.ClientId, u); // acumula mods para siguientes rondas
        // opcional: feedback UI aquí
    }
}
