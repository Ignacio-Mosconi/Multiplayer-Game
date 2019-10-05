using System.Net;
using UnityEngine;

public class UdpNetworkManager : NetworkManager
{
    public IPAddress IPAddress { get; private set; }
    public int Port { get; private set; }
    
    UdpConnection udpConnection;

    public static new UdpNetworkManager Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<UdpNetworkManager>();
            if (!instance)
            {
                GameObject gameObject = new GameObject(typeof(UdpNetworkManager).Name);
                instance = gameObject.AddComponent<UdpNetworkManager>();
            }

            return instance as UdpNetworkManager;
        }
    }

    void Update()
    {
        if (udpConnection != null)
            udpConnection.FlushReceivedData();
    }

    #region Server Methods

    public override void StartServer(int port)
    {
        udpConnection = new UdpServerConnection(port, this);
        IsServer = true;
        Port = port;
    }

    public void SendToClient(byte[] data, IPEndPoint destinationIP)
    {
        UdpServerConnection udpServerConnection = udpConnection as UdpServerConnection;

        if (udpServerConnection != null)
            udpServerConnection.Send(data, destinationIP);
    }

    public void Broadcast(byte[] data)
    {
        UdpServerConnection udpServerConnection = udpConnection as UdpServerConnection;

        if (udpServerConnection != null)
            using (var iterator = UdpConnectionManager.Instance.ClientsIPs.GetEnumerator())
                while (iterator.MoveNext())
                    udpServerConnection.Send(data, iterator.Current);
    }

    #endregion

    #region Client Methods

    public override void StartClient(IPAddress ipAddress, int port)
    {
        udpConnection  = new UdpClientConnection(ipAddress, port, this);
        IsServer = false;
        Port = port;
        IPAddress = ipAddress;
    }

    public void SendToServer(byte[] data)
    {
        UdpClientConnection udpClientConnection = udpConnection as UdpClientConnection;

        if (udpClientConnection != null)
            udpClientConnection.Send(data);
    }

    public void SetClientID(uint clientID)
    {
        UdpClientConnection udpClientConnection = udpConnection as UdpClientConnection;

        if (udpClientConnection != null)
            udpClientConnection.ClientID = clientID;
    }

    #endregion
}