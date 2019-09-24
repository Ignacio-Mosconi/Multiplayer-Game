using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class TcpNetworkManager : NetworkManager
{
    List<TcpConnectedClient> clients = new List<TcpConnectedClient>();
    TcpConnectedClient client;
    IPAddress serverIP;
    TcpListener tcpListener;

    public static new TcpNetworkManager Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<TcpNetworkManager>();
            if (!instance)
            {
                GameObject gameObject = new GameObject(typeof(TcpNetworkManager).Name);
                instance = gameObject.AddComponent<TcpNetworkManager>();
            }

            return instance as TcpNetworkManager;
        }
    }

    void OnDestroy()
    {
        if (tcpListener != null)
            tcpListener.Stop();

        foreach (TcpConnectedClient client in clients)
            client.CloseClient();
    }

    void Update()
    {
        if (IsServer)
            using (var iterator = clients.GetEnumerator())
                while (iterator.MoveNext())
                    iterator.Current.FlushReceivedData();
        else if (client != null)
            client.FlushReceivedData();
    }

    void OnServerConnect(IAsyncResult asyncResult)
    {
        TcpClient tcpClient = tcpListener.EndAcceptTcpClient(asyncResult);
        TcpConnectedClient connectedClient = new TcpConnectedClient(tcpClient, this);

        clients.Add(connectedClient);
        tcpListener.BeginAcceptTcpClient(OnServerConnect, null);
    }

    public override void StartServer(int port)
    {
        IsServer = true;
        tcpListener = new TcpListener(IPAddress.Any, port);

        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(OnServerConnect, null);
    }

    public override void StartClient(IPAddress serverIP, int port)
    {
        IsServer = false;
        this.serverIP = serverIP;

        TcpClient tcpClient = new TcpClient();
        client = new TcpConnectedClient(tcpClient, this);

        tcpClient.BeginConnect(serverIP, port, (ar) => client.OnEndConnection(ar), null);
    }

    public void OnClientDisconnect(TcpConnectedClient client)
    {
        clients.Remove(client);
    }

    public override void Broadcast(byte[] data)
    {
        using (var iterator = clients.GetEnumerator())
            while (iterator.MoveNext())
                iterator.Current.Send(data);
    }

    public override void SendToServer(byte[] data)
    {
        client.Send(data);
    }
}