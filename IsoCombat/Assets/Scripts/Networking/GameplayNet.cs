using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct PlayerState
{
    public string id;
    public string name;
    public float x, y, rotation, scale;
    public bool dead;
}

[Serializable]
public struct PlayerColorMsg
{
    public string playerId;
    public string color;
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

    private Color[] coloresFijos = new Color[] { Color.red, Color.green, Color.yellow, Color.blue };
    private Dictionary<string, Color> coloresAsignados = new Dictionary<string, Color>();
    private int siguienteIndiceColor = 0;

    Color GetColorJugador(string playerId)
    {
        if (coloresAsignados.ContainsKey(playerId))
            return coloresAsignados[playerId];

        Color c = coloresFijos[siguienteIndiceColor % coloresFijos.Length];
        coloresAsignados[playerId] = c;
        siguienteIndiceColor++;
        return c;
    }

    void Start()
    {
        net = SessionConfig.IsHost ? (INetwork)new UDPServer() : new UDPClient();
        net.Port = SessionConfig.Port;
        if (SessionConfig.IsHost) ((UDPServer)net).StartServer(SessionConfig.PlayerName);
        else ((UDPClient)net).StartClient(SessionConfig.ServerIp, SessionConfig.PlayerName);

        NetRuntime.Attach(net);
        net.OnMessage += OnMsg;

        localAvatar = Instantiate(playerPrefab).transform;
        localAvatar.name = $"LOCAL_{SessionConfig.PlayerName}_{SessionConfig.ClientId}";
        pcLocal = localAvatar.GetComponent<PlayerController>();
        pcLocal.isPlayerLocal = true;

        Color colorAsignado = GetColorJugador(SessionConfig.ClientId);
        pcLocal.AssignColor(colorAsignado);

        if (SessionConfig.IsHost)
        {
            net.SendMessage(NetOperation.PLAYER_COLOR, JsonUtility.ToJson(new PlayerColorMsg
            {
                playerId = SessionConfig.ClientId,
                color = ColorUtility.ToHtmlStringRGB(colorAsignado)
            }));
        }

        AddToWinnerList(SessionConfig.ClientId + "_" + SessionConfig.PlayerName);

        var srt = localAvatar.GetComponent<StatsRuntime>();
        var mods = UpgradesState.I.GetFor(SessionConfig.ClientId);
        srt.SetModifiers(mods);

        last[SessionConfig.ClientId] = new PlayerState
        {
            id = SessionConfig.ClientId,
            name = SessionConfig.PlayerName,
            dead = false
        };
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
        if (sendTimer >= 0.01f)
        {
            sendTimer = 0f;
            SendState();
        }
    }

    void SendState(bool immediate = false)
    {
        if (localDead && !immediate) return;

        Vector2 p = localAvatar.position;
        float r = localAvatar.rotation.eulerAngles.z;
        float s = localAvatar.localScale.x;

        var ps = new PlayerState
        {
            id = SessionConfig.ClientId,
            name = SessionConfig.PlayerName,
            x = p.x,
            y = p.y,
            rotation = r,
            scale = s,
            dead = localDead
        };
        last[ps.id] = ps;
        net.SendMessage(NetOperation.STATE, JsonUtility.ToJson(ps));
    }

    void OnMsg(NetMsg m)
    {
        if (m.op == NetOperation.STATE)
        {
            PlayerState ps = JsonUtility.FromJson<PlayerState>(m.payload);
            last[ps.id] = ps;
            if (ps.id == SessionConfig.ClientId) return;

            if (!avatars.TryGetValue(ps.id, out var t) || t == null)
            {
                t = Instantiate(playerPrefab).transform;
                t.name = $"REMOTE_{ps.name}_{ps.id}";
                avatars[ps.id] = t;

                Color c = GetColorJugador(ps.id);
                t.GetComponent<PlayerController>().AssignColor(c);

                AddToWinnerList(ps.id + "_" + ps.name);
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
                t.localScale = new Vector3(ps.scale, ps.scale, ps.scale);
            }

            if (SessionConfig.IsHost) TryEndMatch();
            return;
        }

        if (m.op == NetOperation.PLAYER_COLOR)
        {
            var msg = JsonUtility.FromJson<PlayerColorMsg>(m.payload);
            Color c;
            ColorUtility.TryParseHtmlString("#" + msg.color, out c);

            coloresAsignados[msg.playerId] = c;
            if (avatars.TryGetValue(msg.playerId, out var t) && t != null)
            {
                t.GetComponent<PlayerController>().AssignColor(c);
            }
        }

        if (m.op == NetOperation.BACK_TO_LOBBY)
        {
            net.Stop();
            CircleTransition.instance.CloseBlackScreen("EndGame");
            return;
        }

        if (m.op == NetOperation.FINISH_MATCH)
        {
            net.Stop();
            CircleTransition.instance.CloseBlackScreen("MidRound");
            return;
        }
    }

    void TryEndMatch()
    {
        if (matchEnded) return;
        int vivos = 0; string winner = "";
        foreach (var kv in last)
        {
            if (!kv.Value.dead) { vivos++; winner = kv.Key + "_" + kv.Value.name; }
        }
        if (vivos <= 1)
        {
            matchEnded = true;
            AddToWinnerList(winner);
            NetRuntime.winners[winner] += 1;

            if (NetRuntime.winners[winner] >= 3)
            {
                net.SendMessage(NetOperation.BACK_TO_LOBBY, "");
                net.Stop();
                CircleTransition.instance.CloseBlackScreen("EndGame");
            }
            else
            {
                net.SendMessage(NetOperation.FINISH_MATCH, "");
                net.Stop();
                CircleTransition.instance.CloseBlackScreen("MidRound");
            }
        }
    }

    void AddToWinnerList(string name)
    {
        if (!NetRuntime.winners.ContainsKey(name))
            NetRuntime.winners[name] = 0;
    }
}