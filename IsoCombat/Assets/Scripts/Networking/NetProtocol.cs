using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public enum NetTransport { TCP, UDP }

public enum NetOperation { NULL, HELLO, CHAT, STATE, SYSTEM, RANK_READY, SHOW_RANK, BACK_TO_LOBBY, PLAY, FINISH_MATCH }

[Serializable]
public struct NetMsg
{
    public NetOperation op;
    public long ts;       
    public string clientId;
    public string payload;
}

public static class NetCodec
{
    public static byte[] Encode(NetMsg m, NetTransport t)
    {
        Normalize(ref m);
        string json = JsonUtility.ToJson(m);
        byte[] body = Encoding.UTF8.GetBytes(json);

        if (t == NetTransport.UDP) return body;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(body.Length);                  
        bw.Write(body);
        return ms.ToArray();
    }

    public static bool TryDecodeTcp(ref List<byte> buf, out NetMsg msg)
    {
        msg = default;
        if (buf.Count < 4) return false;
        int len = BitConverter.ToInt32(buf.GetRange(0, 4).ToArray(), 0);
        if (len < 0 || buf.Count < 4 + len) return false;

        string json = Encoding.UTF8.GetString(buf.GetRange(4, len).ToArray());
        msg = JsonUtility.FromJson<NetMsg>(json);

        buf.RemoveRange(0, 4 + len);
        return !(msg.op == NetOperation.NULL);
    }

    public static bool TryDecodeUdp(byte[] data, int count, out NetMsg msg)
    {
        msg = default;
        try
        {
            string json = Encoding.UTF8.GetString(data, 0, count);
            msg = JsonUtility.FromJson<NetMsg>(json);
            return !(msg.op == NetOperation.NULL);
        }
        catch { return false; }
    }

    static void Normalize(ref NetMsg m)
    {
        if (m.ts == 0) m.ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (string.IsNullOrEmpty(m.clientId)) m.clientId = SessionConfig.ClientId;
    }
}
