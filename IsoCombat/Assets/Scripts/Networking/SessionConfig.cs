using UnityEngine;

public static class SessionConfig
{
    public static TransportType Transport = TransportType.TCP;
    public static string PlayerName = "Player";
    public static int Port = 9050;

    public static bool IsHost = false;
    public static string ServerIp = "127.0.0.1";
}