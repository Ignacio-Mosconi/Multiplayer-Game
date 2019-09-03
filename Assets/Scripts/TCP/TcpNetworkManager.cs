using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class TcpNetworkManager : MonoBehaviourSingleton<TcpNetworkManager>,  IDataReceiver
{
    List<TcpConnectedClient> clients = new List<TcpConnectedClient>();
    TcpConnectedClient client;
    IPAddress serverIP;
    TcpListener tcpListener;

    public Action<byte[], IPEndPoint> OnReceiveData;
    public bool IsServer { get; private set; }

    void OnApplicationQuit()
    {
        tcpListener?.Stop();
        using (var iterator = clients.GetEnumerator())
            while (iterator.MoveNext())
                iterator.Current.CloseClient();
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

    public void StartServer(int port)
    {
        IsServer = true;
        tcpListener = new TcpListener(IPAddress.Any, port);
        
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(OnServerConnect, null);
    }

    public void StartClient(IPAddress serverIP, int port)
    {
        IsServer = false;
        this.serverIP = serverIP;

        TcpClient tcpClient = new TcpClient();
        client = new TcpConnectedClient(tcpClient, this);

        tcpClient.BeginConnect(serverIP, port, (ar) => client.OnEndConnection(ar), null);
    }

    public void ReceiveData(byte[] data, IPEndPoint ipEndPoint = null)
    {
        if (OnReceiveData != null)
            OnReceiveData.Invoke(data, ipEndPoint);
    }

    public void OnClientDisconnect(TcpConnectedClient client)
    {
        clients.Remove(client);
    }

    public void Broadcast(byte[] data)
    {
        using (var iterator = clients.GetEnumerator())
            while (iterator.MoveNext())
                iterator.Current.Send(data);
    }

    public void SendMessageToServer(byte[] data)
    {
        client.Send(data);
    }
}