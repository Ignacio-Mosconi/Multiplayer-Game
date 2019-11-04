using System;
using System.Net;

public abstract class ConnectionManager : MonoBehaviourSingleton<ConnectionManager>
{
    protected Action onClientConnectedCallback;

    public abstract void CreateServer(int port);
    public abstract void ConnectToServer(IPAddress ipAddress, int port, Action onClientConnectedCallback = null);
}