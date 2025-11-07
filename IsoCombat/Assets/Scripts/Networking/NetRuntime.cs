using System.Collections.Generic;
using UnityEngine;

public class NetRuntime : MonoBehaviour
{
    public static INetwork Net;

    public static Dictionary<string, int> winners = new Dictionary<string, int>();

    public static void Attach(INetwork net)
    {
        if (NetRuntime.Net != null && NetRuntime.Net != net)
        {
            try { NetRuntime.Net.Stop(); } catch { }
        }

        if (NetRuntime.Net == null)
        {
            var go = new GameObject("NetRuntime");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<NetRuntime>();
        }
        Net = net;

    }

    public static void ResetWinners()
    {
        if (winners != null && winners.Count > 0)
        {
            winners.Clear(); 
        }
    }


    void OnDestroy() { Net?.Stop(); Net = null; }
}
