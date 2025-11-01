using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PlayerState
{
    public string id;
    public string name;
    public float x, y, rotation;
    public bool dead;
}

[Serializable] 
public struct GameEvent { 
    public string type; 
    public string id; 
}

public class GameplayNet : MonoBehaviour
{
    public GameObject playerPrefab;
    public bool localDead = false;

    INetwork net;
    readonly Dictionary<string, Transform> avatars = new();
    readonly Dictionary<string, PlayerState> last = new();
    Transform localAvatar;
    PlayerController pcLocal;
    float sendTimer;
    bool matchEnded;

    void Start()
    {
        var udp = SessionConfig.IsHost ? (INetwork)new UDPServer()
                                       : (INetwork)new UDPClient();
        udp.Port = SessionConfig.Port;
        if (SessionConfig.IsHost) udp.StartServer(SessionConfig.PlayerName);
        else udp.StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        NetRuntime.Attach(udp);
        net = udp;

        net.OnMessage -= OnMsg;
        net.OnMessage += OnMsg;

        if (localAvatar == null && playerPrefab != null)
        {
            localAvatar = Instantiate(playerPrefab).transform;
            localAvatar.name = $"LOCAL_{SessionConfig.PlayerName}_{SessionConfig.ClientId}";
            pcLocal = localAvatar.GetComponent<PlayerController>();
            pcLocal.isPlayerLocal = true;

            last[SessionConfig.ClientId] = new PlayerState
            {
                id = SessionConfig.ClientId,
                name = SessionConfig.PlayerName,
                dead = false
            };
        }
    }

    void OnDestroy()
    {
        if (net != null) net.OnMessage -= OnMsg;
    }

    void Update()
    {
        net?.Tick();
        if (matchEnded) return;

        if (localAvatar == null) return;

        if (!localDead && pcLocal.isDead)
        {
            localDead = true;
            SendState(true);
            pcLocal.enabled = false;
            if (SessionConfig.IsHost) TryEndMatch();
        }

        sendTimer += Time.deltaTime;
        if (sendTimer >= 0.01f) // <-- Limite para enviar datos cada x tiempo
        {
            sendTimer = 0f;
            SendState();
        }
    }

    void SendState(bool immediate = false)
    {
        if (localDead && !immediate) return; // muerto: solo se envió el STATE de muerte
        Vector2 p = localAvatar.position;
        float r = localAvatar.rotation.eulerAngles.z;

        var ps = new PlayerState
        {
            id = SessionConfig.ClientId,
            name = SessionConfig.PlayerName,
            x = p.x,
            y = p.y,
            rotation = r,
            dead = localDead
        };
        last[ps.id] = ps;
        net.SendMessage(NetOperation.STATE, JsonUtility.ToJson(ps));
    }

    void OnMsg(NetMsg m)
    {
        if (m.op != NetOperation.STATE && m.op != NetOperation.SYSTEM) return;

        if(m.op == NetOperation.SYSTEM)
        {
            if (m.op == NetOperation.SYSTEM && m.payload.StartsWith("__BACK_TO_LOBBY__"))
                UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
            return;
        }



        PlayerState ps = JsonUtility.FromJson<PlayerState>(m.payload);
        last[ps.id] = ps;

        if (ps.id == SessionConfig.ClientId) return;

        if (!avatars.TryGetValue(ps.id, out var t) || t == null)
        {
            t = Instantiate(playerPrefab).transform;
            t.name = $"REMOTE_{ps.name}_{ps.id}";
            avatars[ps.id] = t;
        }


        if (ps.dead)
        {
            SpriteRenderer r = t.GetComponentInChildren<SpriteRenderer>(); if (r) r.enabled = false;
            PlayerController pc = t.GetComponent<PlayerController>(); if (pc) pc.enabled = false;
        }
        else
        {
            t.position = new Vector3(ps.x, ps.y, 0f);
            t.rotation = Quaternion.Euler(0f, 0f, ps.rotation);
        }

        if (SessionConfig.IsHost) TryEndMatch();

    }

    void TryEndMatch()
    {
        if (matchEnded) return;
        int vivos = 0; string winner = "";
        foreach (var kv in last)
        {
            if (!kv.Value.dead) { vivos++; winner = kv.Key; }
        }
        if (vivos <= 1)
        {
            matchEnded = true;
            net.SendMessage(NetOperation.SYSTEM, $"__BACK_TO_LOBBY__|{winner}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby"); // host también vuelve
        }
    }
}
