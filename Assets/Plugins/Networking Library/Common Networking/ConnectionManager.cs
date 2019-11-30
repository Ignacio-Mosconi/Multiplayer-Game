using System;
using System.Net;

public abstract class ConnectionManager : MonoBehaviourSingleton<ConnectionManager>
{
    protected Action<uint> onClientConnectedCallback;

    public abstract void CreateServer(int port);
    public abstract void ConnectToServer(IPAddress ipAddress, int port, Action<uint> onClientConnectedCallback = null);
}