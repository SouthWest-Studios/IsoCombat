using JetBrains.Annotations;
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
    public List<BulletState> bullets;
    
}

[Serializable]
public struct BulletState {
    public string id;
    public float bulletX, bulletY, bulletRotation, bulletScale;
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

[Serializable]
public struct BulletHitMsg
{
    public string ownerId;
    public string bulletId;
}

public class GameplayNet : MonoBehaviour
{
    public static GameplayNet I;

    public GameObject playerPrefab;
    public GameObject bulletPrefab;
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
    readonly Dictionary<string, Dictionary<string, Rigidbody2D>> remoteBullets = new();
    Transform localAvatar;
    PlayerController pcLocal;
    float sendTimer;
    bool matchEnded;

    private Color[] coloresFijos = new Color[] { Color.red, Color.green, Color.yellow, Color.blue };
    private int siguienteIndiceColor = 0;

    Color GetColorJugador(string playerId)
    {
        if (NetRuntime.colors.TryGetValue(playerId, out var existing))
            return existing;

        Color c = coloresFijos[siguienteIndiceColor % coloresFijos.Length];
        NetRuntime.colors[playerId] = c;
        siguienteIndiceColor++;
        return c;
    }

    private void Awake()
    {
        I = this;
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

            pcLocal = localAvatar.GetComponent<PlayerController>();
            pcLocal.isPlayerLocal = true;
        }
        else
        {
            localAvatar = null;
            UnityEngine.Debug.Log("[GameplayNet] Espectador: no se instancia avatar local.");
        }

        
        if (SessionConfig.IsHost)
        {

            Color myColor = GetColorJugador(SessionConfig.ClientId);
            if (pcLocal != null) pcLocal.AssignColor(myColor);
            BroadcastAllColors();
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

        PlayerController pcLocal = localAvatar.GetComponent<PlayerController>();

        Vector2 p = localAvatar.position;
        float r = localAvatar.rotation.eulerAngles.z;
        float s = localAvatar.localScale.x;

        //Bullets
        List<BulletState> bullets = new List<BulletState>();
        foreach(Rigidbody2D bulletRB in pcLocal.bullets)
        {
            BulletNetInfo info = bulletRB.GetComponent<BulletNetInfo>();

            bullets.Add(new BulletState {
                id = info != null ? info.bulletId : bulletRB.GetInstanceID().ToString(),
                bulletRotation = bulletRB.rotation,
                bulletScale = bulletRB.transform.localScale.x,
                bulletX = bulletRB.transform.position.x,
                bulletY = bulletRB.transform.position.y
            });
        }
        
        //PlayerStats
        var ps = new PlayerState
        {
            id = SessionConfig.ClientId,
            name = SessionConfig.PlayerName,
            x = p.x,
            y = p.y,
            rotation = r,
            scale = s,
            damaged = pcLocal.haveDamage,
            dead = localDead,
            bullets = bullets,
        };

        last[ps.id] = ps;
        net.SendMessage(NetOperation.STATE, JsonUtility.ToJson(ps));
        pcLocal.haveDamage = 0;
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

                AddToWinnerList(ps.id + "_" + ps.name);

                //Color
                if (SessionConfig.IsHost)
                {
                    Color nuevoColor = GetColorJugador(ps.id);
                    t.GetComponent<PlayerController>().AssignColor(nuevoColor);
                    BroadcastAllColors();
                }
                else
                {
                    if (NetRuntime.colors.TryGetValue(ps.id, out var c))
                        t.GetComponent<PlayerController>().AssignColor(c);
                }
            }

            if (!remoteBullets.TryGetValue(ps.id, out var playerBullets))
            {
                playerBullets = new Dictionary<string, Rigidbody2D>();
                remoteBullets[ps.id] = playerBullets;
            }

            HashSet<string> idsRecibidos = new HashSet<string>();
            var bulletsList = ps.bullets ?? new List<BulletState>();
            foreach (BulletState b in bulletsList)
            {
                idsRecibidos.Add(b.id);

                if (!playerBullets.TryGetValue(b.id, out var rb) || rb == null)
                {
                    GameObject go = Instantiate(bulletPrefab);
                    rb = go.GetComponent<Rigidbody2D>();
                    rb.bodyType = RigidbodyType2D.Kinematic;

                    BulletNetInfo info = go.GetComponent<BulletNetInfo>();
                    if (info != null)
                    {
                        info.ownerId = ps.id;
                        info.bulletId = b.id;
                    }

                    playerBullets[b.id] = rb;
                }

                rb.transform.position = new Vector3(b.bulletX, b.bulletY, 0f);
                rb.transform.rotation = Quaternion.Euler(0f, 0f, b.bulletRotation);
                rb.transform.localScale = Vector3.one * b.bulletScale;
            }

            List<string> idsLocales = new List<string>(playerBullets.Keys);
            foreach (string bulletId in idsLocales)
            {
                if (!idsRecibidos.Contains(bulletId))
                {
                    Rigidbody2D rb = playerBullets[bulletId];
                    if (rb != null)
                        UnityEngine.Object.Destroy(rb.gameObject);

                    playerBullets.Remove(bulletId);
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
            var all = JsonUtility.FromJson<AllPlayerColorsMsg>(m.payload);
            foreach (var p in all.players)
            {
                if (ColorUtility.TryParseHtmlString("#" + p.color, out Color c))
                {
                    NetRuntime.colors[p.playerId] = c;
                    if (avatars.TryGetValue(p.playerId, out var t) && t != null)
                        t.GetComponent<PlayerController>().AssignColor(c);
                    else if (p.playerId == SessionConfig.ClientId && pcLocal != null)
                        pcLocal.AssignColor(c);
                }
            }
            return;
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

        if (m.op == NetOperation.BULLET_HIT)
        {
            var hit = JsonUtility.FromJson<BulletHitMsg>(m.payload);
            HandleBulletHit(hit);
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
            NetRuntime.lastWinner = winner;

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

    void BroadcastAllColors()
    {
        var allColors = new AllPlayerColorsMsg { players = new List<PlayerColorMsg>() };
        foreach (var kv in NetRuntime.colors)
        {
            allColors.players.Add(new PlayerColorMsg
            {
                playerId = kv.Key,
                color = ColorUtility.ToHtmlStringRGB(kv.Value)
            });
        }
        net.SendMessage(NetOperation.PLAYER_COLOR, JsonUtility.ToJson(allColors));
    }

    void HandleBulletHit(BulletHitMsg hit)
    {

        if (SessionConfig.ClientId == hit.ownerId && localAvatar != null)
        {
            var pcLocal = localAvatar.GetComponent<PlayerController>();
            if (pcLocal != null && pcLocal.bullets != null)
            {
                Rigidbody2D toRemove = null;

                foreach (var rb in pcLocal.bullets)
                {
                    var info = rb.GetComponent<BulletNetInfo>();
                    if (info != null && info.bulletId == hit.bulletId)
                    {
                        toRemove = rb;
                        break;
                    }
                }

                if (toRemove != null)
                {
                    pcLocal.bullets.Remove(toRemove);
                    Destroy(toRemove.gameObject);
                }
            }
        }

        if (remoteBullets.TryGetValue(hit.ownerId, out var playerBullets))
        {
            if (playerBullets.TryGetValue(hit.bulletId, out var rb) && rb != null)
            {
                Destroy(rb.gameObject);
                playerBullets.Remove(hit.bulletId);
            }
        }
    }

    public void SendMessage(NetOperation op, string payloadJsonOrText)
    {
        net.SendMessage(op, payloadJsonOrText);
    }

    public void BulletHit(BulletNetInfo info)
    {
        if (info == null) return;
        BulletHitMsg msg = new BulletHitMsg
        {
            ownerId = info.ownerId,
            bulletId = info.bulletId
        };

        string json = JsonUtility.ToJson(msg);
        NetRuntime.Net.SendMessage(NetOperation.BULLET_HIT, json);
    }
}