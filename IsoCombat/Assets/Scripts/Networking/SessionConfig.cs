using UnityEngine;

public static class SessionConfig
{
    public static string PlayerName = "Player";
    public static int Port = 9050;
    public static bool IsHost = false;
    public static string ServerIp = "127.0.0.1";
    public static bool IsSpectator = false;

    public static readonly string ClientId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
}
