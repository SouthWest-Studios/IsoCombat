using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PlayerState
{
    public string id;
    public string name;
    public float x, y, z;
}

public class GameplayNet : MonoBehaviour
{
    public GameObject playerPrefab;

    INetwork net;
    readonly Dictionary<string, Transform> avatars = new();
    Transform localAvatar;
    float sendTimer;

    void Start()
    {
        var udp = SessionConfig.IsHost ? (INetwork)new UDPServer()
                                       : (INetwork)new UDPClient();
        udp.Port = SessionConfig.Port;
        if (SessionConfig.IsHost) udp.StartServer(SessionConfig.PlayerName);
        else udp.StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        NetRuntime.Attach(udp);
        net = udp;

        net.OnSystemMessage -= OnSystem;
        net.OnSystemMessage += OnSystem;

        if (localAvatar == null && playerPrefab != null)
        {
            localAvatar = Instantiate(playerPrefab).transform;
            localAvatar.name = $"LOCAL_{SessionConfig.PlayerName}_{SessionConfig.ClientId}";
            localAvatar.GetComponent<PlayerController>().isPlayerLocal = true;
        }
    }

    void OnDestroy()
    {
        if (net != null) net.OnSystemMessage -= OnSystem;
    }

    void Update()
    {
        net?.Tick();

        if (localAvatar == null) return;

        sendTimer += Time.deltaTime;
        if (sendTimer >= 0.01f)
        {
            sendTimer = 0f;
            var p = localAvatar.position;
            var r = localAvatar.rotation.eulerAngles.z;

            var ps = new PlayerState
            {
                id = SessionConfig.ClientId,
                name = SessionConfig.PlayerName,
                x = p.x,
                y = p.y,
                z = r
            };
            string json = JsonUtility.ToJson(ps);
            net.Send("SYSTEM:STATEJSON|" + json);
        }
    }

    void OnSystem(string s)
    {
        int k = s.IndexOf("STATEJSON|", StringComparison.Ordinal);
        if (k < 0) return;

        string json = s.Substring(k + "STATEJSON|".Length);

        PlayerState ps;
        try { ps = JsonUtility.FromJson<PlayerState>(json); }
        catch (Exception e) { Debug.LogError($"JSON ERROR {e}"); return; }

        if (ps.id == SessionConfig.ClientId) return;

        if (!avatars.TryGetValue(ps.id, out var t) || t == null)
        {
            t = Instantiate(playerPrefab).transform;
            t.name = $"REMOTE_{ps.name}_{ps.id}";
            avatars[ps.id] = t;
        }
        t.position = new Vector3(ps.x, ps.y, 0f);
        t.rotation = Quaternion.Euler(0f, 0f, ps.z);
    }
}
