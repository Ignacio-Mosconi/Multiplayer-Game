using System.Net;
using System.Collections.Generic;
using UnityEngine;

public struct UdpClientData
{
    public IPEndPoint ipEndPoint;
    public uint id;
    public float timestamp;

    public UdpClientData(IPEndPoint ipEndPoint, uint id, float timestamp)
    {
        this.ipEndPoint = ipEndPoint;
        this.id = id;
        this.timestamp = timestamp;
    }
}

public class UdpNetworkManager : NetworkManager
{
    public IPAddress IPAddress { get; private set; }
    public int Port { get; private set; }
    public uint ClientID { get; private set; }

    Dictionary<uint, UdpClientData> udpClientsData = new Dictionary<uint, UdpClientData>();
    Dictionary<IPEndPoint, uint> udpClientsIDs = new Dictionary<IPEndPoint, uint>();
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

    public override void Broadcast(byte[] data)
    {
        UdpServerConnection udpServerConnection = udpConnection as UdpServerConnection;

        if (udpServerConnection != null)
            using (var iterator = udpClientsData.GetEnumerator())
                while (iterator.MoveNext())
                    udpServerConnection.Send(data, iterator.Current.Value.ipEndPoint);

    }

    #endregion

    #region Client Methods

    void AddClient(IPEndPoint ipEndPoint)
    {
        UdpClientData udpClientData = new UdpClientData(ipEndPoint, ClientID, Time.realtimeSinceStartup);

        udpClientsIDs[ipEndPoint] = ClientID;
        udpClientsData.Add(ClientID, udpClientData);

        ClientID++;
    }

    void RemoveClient(IPEndPoint ipEndPoint)
    {
        if (!udpClientsIDs.ContainsKey(ipEndPoint))
        {
            Debug.LogWarning("Cannot remove the client because there are none with the given IP End Point.", gameObject);
            return;
        }

        udpClientsIDs.Remove(ipEndPoint);
    }

    public override void StartClient(IPAddress ipAddress, int port)
    {
        udpConnection = new UdpClientConnection(ipAddress, port, this);
        IsServer = false;
        Port = port;
        IPAddress = ipAddress;
    }

    public override void SendToServer(byte[] data)
    {
        UdpClientConnection udpClientConnection = udpConnection as UdpClientConnection;

        if (udpClientConnection != null)
            udpClientConnection.Send(data);
    }

    #endregion

    public override void ReceiveData(byte[] data, IPEndPoint ipEndPoint)
    {
        if (!udpClientsIDs.ContainsKey(ipEndPoint))
            AddClient(ipEndPoint);
        
        base.ReceiveData(data, ipEndPoint);
    }
}