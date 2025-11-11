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
    public float damaged;
    public bool dead;
}

[Serializable]
public struct PlayerColorMsg
{
    public string playerId;
    public string color;
}

[Serializable]
public struct AllPlayerColorsMsg
{
    public List<PlayerColorMsg> players;
}

[Serializable]
public struct SpawnAssignment
{
    public string playerId;
    public int spawnIndex;
}

public class GameplayNet : MonoBehaviour
{
    public GameObject playerPrefab;
    public bool localDead = false;

    [SerializeField] private Transform spawn0;
    [SerializeField] private Transform spawn1;
    [SerializeField] private Transform spawn2;
    [SerializeField] private Transform spawn3;

    public Transform GetSpawn(int index)
    {
        switch (index)
        {
            case 0: return spawn0;
            case 1: return spawn1;
            case 2: return spawn2;
            case 3: return spawn3;
            default: return null;
        }
    }

    public void SetPlayerAtSpawn(GameObject player, int index)
    {
        var t = GetSpawn(index);
        player.transform.SetPositionAndRotation(t.position, t.rotation);
    }

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

        if (!SessionConfig.IsSpectator)
        {
            localAvatar = Instantiate(playerPrefab).transform;
            localAvatar.name = $"LOCAL_{SessionConfig.PlayerName}_{SessionConfig.ClientId}";

            int spawnIndex = Mathf.Abs(SessionConfig.ClientId.GetHashCode()) % 4;
            SetPlayerAtSpawn(localAvatar.gameObject, spawnIndex);
        }
        else
        {
            localAvatar = null;
            UnityEngine.Debug.Log("[GameplayNet] Espectador: no se instancia avatar local.");
        }

        if (localAvatar != null)
        {
            pcLocal = localAvatar.GetComponent<PlayerController>();
            pcLocal.isPlayerLocal = true;

            // Asignar color local
            Color colorAsignado = GetColorJugador(SessionConfig.ClientId);
            pcLocal.AssignColor(colorAsignado);
        }

        // Solo el host decide y comunica los colores
        if (SessionConfig.IsHost)
        {
            // Asegura que el host tenga color asignado, aunque no exista avatar local
            if (!coloresAsignados.TryGetValue(SessionConfig.ClientId, out var myColor))
            {
                myColor = GetColorJugador(SessionConfig.ClientId);
                coloresAsignados[SessionConfig.ClientId] = myColor;
            }

            // Si hay avatar local, aplícale su color
            if (pcLocal != null)
                pcLocal.AssignColor(myColor);

            // Construye y envía la tabla completa de colores
            var allColors = new AllPlayerColorsMsg { players = new List<PlayerColorMsg>() };
            foreach (var kv in coloresAsignados)
            {
                allColors.players.Add(new PlayerColorMsg
                {
                    playerId = kv.Key,
                    color = ColorUtility.ToHtmlStringRGB(kv.Value)
                });
            }

            net.SendMessage(NetOperation.PLAYER_COLOR, JsonUtility.ToJson(allColors));

            // Reaplica colores a avatares que ya estén en escena
            foreach (var kv in coloresAsignados)
            {
                if (kv.Key == SessionConfig.ClientId)
                {
                    if (pcLocal != null) pcLocal.AssignColor(kv.Value);
                    continue;
                }
                if (avatars.TryGetValue(kv.Key, out var t) && t != null)
                    t.GetComponent<PlayerController>().AssignColor(kv.Value);
            }
        }


        AddToWinnerList(SessionConfig.ClientId + "_" + SessionConfig.PlayerName);

        if (localAvatar != null)
        {
            var srt = localAvatar.GetComponent<StatsRuntime>();
            var mods = UpgradesState.I.GetFor(SessionConfig.ClientId);
            srt.SetModifiers(mods);
        }

        if (!SessionConfig.IsSpectator)
        {
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

        if (localAvatar != null)
        {
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
            damaged = localAvatar.GetComponent<PlayerController>().haveDamage,
            dead = localDead
        };

        last[ps.id] = ps;
        net.SendMessage(NetOperation.STATE, JsonUtility.ToJson(ps));
        localAvatar.GetComponent<PlayerController>().haveDamage = 0;
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

                // Si ya tenemos el color asignado por el servidor, lo aplicamos
                if (coloresAsignados.TryGetValue(ps.id, out var c))
                    t.GetComponent<PlayerController>().AssignColor(c);
                else
                    Debug.LogWarning($"Color del jugador {ps.id} a�n no recibido del servidor");

                AddToWinnerList(ps.id + "_" + ps.name);

                // Si somos el host, asignamos color y lo enviamos a todos
                if (SessionConfig.IsHost)
                {
                    Color nuevoColor = GetColorJugador(ps.id);
                    coloresAsignados[ps.id] = nuevoColor;

                    var allColors = new AllPlayerColorsMsg { players = new List<PlayerColorMsg>() };
                    foreach (var kv in coloresAsignados)
                    {
                        allColors.players.Add(new PlayerColorMsg
                        {
                            playerId = kv.Key,
                            color = ColorUtility.ToHtmlStringRGB(kv.Value)
                        });
                    }
                    net.SendMessage(NetOperation.PLAYER_COLOR, JsonUtility.ToJson(allColors));
                }
            }

            if (ps.dead)
            {
                SpriteRenderer r = t.GetComponentInChildren<SpriteRenderer>();
                if (r) r.enabled = false;
                PlayerController pc = t.GetComponent<PlayerController>();
                if (pc) pc.enabled = false;
            }
            else
            {
                t.position = new Vector3(ps.x, ps.y, 0f);
                t.rotation = Quaternion.Euler(0f, 0f, ps.rotation);
                t.localScale = new Vector3(ps.scale, ps.scale, ps.scale);
                t.GetComponent<PlayerController>().SetHealth(ps.damaged);
            }

            if (SessionConfig.IsHost) TryEndMatch();
            return;
        }

        if (m.op == NetOperation.PLAYER_COLOR)
        {
            try
            {
                var all = JsonUtility.FromJson<AllPlayerColorsMsg>(m.payload);
                foreach (var p in all.players)
                {
                    if (ColorUtility.TryParseHtmlString("#" + p.color, out Color c))
                    {
                        coloresAsignados[p.playerId] = c;
                        if (avatars.TryGetValue(p.playerId, out var t) && t != null)
                            t.GetComponent<PlayerController>().AssignColor(c);
                        else if (p.playerId == SessionConfig.ClientId && pcLocal != null)
                            pcLocal.AssignColor(c);
                    }
                }
            }
            catch
            {
                var msg = JsonUtility.FromJson<PlayerColorMsg>(m.payload);
                if (ColorUtility.TryParseHtmlString("#" + msg.color, out Color c))
                {
                    coloresAsignados[msg.playerId] = c;
                    if (avatars.TryGetValue(msg.playerId, out var t) && t != null)
                        t.GetComponent<PlayerController>().AssignColor(c);
                    else if (msg.playerId == SessionConfig.ClientId && pcLocal != null)
                        pcLocal.AssignColor(c);
                }
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
        int vivos = 0;
        string winner = "";
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

    public void SendMessage(NetOperation op, string payloadJsonOrText)
    {
        net.SendMessage(op, payloadJsonOrText);
    }
}