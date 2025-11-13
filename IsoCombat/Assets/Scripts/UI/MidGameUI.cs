using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class MidGameUI : MonoBehaviour
{
    public Upgrade[] pool;
    public Button[] choices;
    Upgrade[] offered;

    public RectTransform textWaiting;

    [Header("Pesos por estrella (1..5)")]
    public int w1 = 30, w2 = 25, w3 = 20, w4 = 15, w5 = 10;
    public Color c1 = Color.white, c2 = Color.green, c3 = Color.blue, c4 = Color.magenta, c5 = Color.red;

    public static MidGameUI I;
    private void Awake()
    {
        I = this;
    }
    void Start()
    {
        if (SessionConfig.IsSpectator || NetRuntime.lastWinner == SessionConfig.ClientId + "_" + SessionConfig.PlayerName)
        {
            DesactivarMejoras();
            return;
        }
        textWaiting.gameObject.SetActive(false);
        offered = DrawThree(pool, new int[] { w1, w2, w3, w4, w5 });

        for (int i = 0; i < choices.Length; i++)
        {
            var btn = choices[i];
            var has = i < offered.Length;
            btn.interactable = has;
            if (!has) continue;

            int idx = i;
            btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = offered[i].title;
            btn.GetComponentInChildren<Image>().sprite = offered[i].icon;
            PaintButtonByStars(btn, offered[i].starsRarity);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => Pick(offered[idx]));
        }
    }

    public void DesactivarMejoras()
    {
        foreach (var b in choices) if (b) b.gameObject.SetActive(false);
        textWaiting.gameObject.SetActive(true);
    }

    void Pick(Upgrade u)
    {
        if (SessionConfig.IsSpectator) return;
        
        var p = new UpgradePick
        {
            playerId = SessionConfig.ClientId,
            mods = u.entries.ToArray()
        };
        MidGameNet.I.SendUpgradePicked(p);
        //foreach (var b in choices) b.interactable = false;
        foreach (var b in choices) if (b) b.gameObject.SetActive(false);
        textWaiting.gameObject.SetActive(true);
    }

    Color GetColorByStar(int star)
    {
        switch (star)
        {
            case 1: return c1;
            case 2: return c2;
            case 3: return c3;
            case 4: return c4;
            case 5: return c5;
        }
        return c1;
    }

    void PaintButtonByStars(Button btn, int stars)
    {
        
        btn.transition = Selectable.Transition.ColorTint;

        var cb = btn.colors;                 
        var c = GetColorByStar(stars);      
        cb.normalColor = c;
        cb.highlightedColor = c;              
        cb.pressedColor = c * 0.9f;       
        cb.selectedColor = c;
        cb.disabledColor = new Color(c.r, c.g, c.b, 0.3f);
        cb.colorMultiplier = 1f;             

        btn.colors = cb;                     
    }



    Upgrade[] DrawThree(Upgrade[] all, int[] starWeights)
    {
        var buckets = new Dictionary<int, List<Upgrade>>();
        for (int s = 1; s <= 5; s++) buckets[s] = new List<Upgrade>();
        foreach (var u in all)
        {
            int s = Mathf.Clamp(u.starsRarity, 1, 5);
            buckets[s].Add(u);
        }

        var result = new List<Upgrade>(3);
        for (int n = 0; n < 3; n++)
        {
            
            Upgrade pick = null;
            const int maxTries = 10;
            for (int t = 0; t < maxTries; t++)
            {
                int s = RollStar(starWeights); 
                var bucket = buckets[s];

                if (bucket.Count == 0)
                {
                    if (AllBucketsEmpty(buckets)) break;
                    continue;
                }

                pick = bucket[Random.Range(0, bucket.Count)];
                bucket.Remove(pick);
                break;
            }


            if (pick == null && all.Length > 0)
            {
                var candidates = all.Except(result).ToArray();
                if (candidates.Length == 0) break;
                pick = candidates[Random.Range(0, candidates.Length)];
            }

            if (pick != null) result.Add(pick);
        }

        return result.ToArray();
    }

    int RollStar(int[] w)
    {
        int total = 0; for (int i = 0; i < 5; i++) total += Mathf.Max(0, w[i]);
        if (total <= 0) return 1;

        int r = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < 5; i++)
        {
            acc += Mathf.Max(0, w[i]);
            if (r < acc) return i + 1;
        }
        return 5;
    }

    bool AllBucketsEmpty(Dictionary<int, List<Upgrade>> buckets)
    {
        foreach (var kv in buckets) if (kv.Value.Count > 0) return false;
        return true;
    }
}
