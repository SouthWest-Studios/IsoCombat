using UnityEngine;

public class NetRuntime : MonoBehaviour
{
    public static INetwork Net;

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

    void OnDestroy() { Net?.Stop(); Net = null; }
}
