using System;
using System.Net;
using UnityEngine;

public class TcpConnectionManager : ConnectionManager
{
    bool hasToTriggerConnectionCallback;

    public static new TcpConnectionManager Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<TcpConnectionManager>();
            if (!instance)
            {
                GameObject gameObject = new GameObject(typeof(TcpConnectionManager).Name);
                instance = gameObject.AddComponent<TcpConnectionManager>();
            }

            return instance as TcpConnectionManager;
        }
    }

    void Update()
    {
        if (hasToTriggerConnectionCallback)
        {
            onClientConnectedCallback?.Invoke();
            hasToTriggerConnectionCallback = false;
        }
    }

    public override void CreateServer(int port)
    {
        TcpNetworkManager.Instance.StartServer(port);
    }

    public override void ConnectToServer(IPAddress ipAddress, int port, Action onClientConnectedCallback = null)
    {
        this.onClientConnectedCallback = onClientConnectedCallback;
        TcpNetworkManager.Instance.OnClientConnected += () => hasToTriggerConnectionCallback = true;
        TcpNetworkManager.Instance.StartClient(ipAddress, port);
    }
}