using System;
using System.Net;
using System.Collections.Generic;
using UnityEngine;

public struct UdpClientData
{
    public IPEndPoint ipEndPoint;
    public int id;
    public float timestamp;

    public UdpClientData(IPEndPoint ipEndPoint, int id, float timestamp)
    {
        this.ipEndPoint = ipEndPoint;
        this.id = id;
        this.timestamp = timestamp;
    }
}

public class UdpNetworkManager : MonoBehaviourSingleton<UdpNetworkManager>, IDataReceiver
{
    public IPAddress IPAddress { get; private set; }
    public bool IsServer { get; private set; }
    public int Port { get; private set; }

    public Action<byte[], IPEndPoint> OnReceiveData;

    Dictionary<int, UdpClientData> udpClientsData = new Dictionary<int, UdpClientData>();
    Dictionary<IPEndPoint, int> udpClientsIDs = new Dictionary<IPEndPoint, int>();
    UdpConnection udpConnection;
    int clientID = 0;

    void Update()
    {
        if (udpConnection != null)
            udpConnection.FlushReceivedData();
    }

    #region Server Methods

    public void StartServer(int port)
    {
        udpConnection = new UdpServerConnection(port, this);
        IsServer = true;
        Port = port;
    }

    public void Broadcast(byte[] data)
    {
        UdpServerConnection udpServerConnection = udpConnection as UdpServerConnection;

        if (udpServerConnection != null)
            using (var iterator = udpClientsData.GetEnumerator())
                while (iterator.MoveNext())
                    udpServerConnection.Send(data, iterator.Current.Value.ipEndPoint);

    }

    #endregion

    #region Client Methods

    public void StartClient(IPAddress ipAddress, int port)
    {
        udpConnection = new UdpClientConnection(ipAddress, port, this);
        IsServer = false;
        Port = port;
        IPAddress = ipAddress;
    }

    public void AddClient(IPEndPoint ipEndPoint)
    {
        UdpClientData udpClientData = new UdpClientData(ipEndPoint, clientID, Time.realtimeSinceStartup);

        udpClientsIDs[ipEndPoint] = clientID;
        udpClientsData.Add(clientID, udpClientData);

        clientID++;
    }

    public void RemoveClient(IPEndPoint ipEndPoint)
    {
        if (!udpClientsIDs.ContainsKey(ipEndPoint))
        {
            Debug.LogWarning("Cannot remove the client because there are none with the given IP End Point.", gameObject);
            return;
        }

        udpClientsIDs.Remove(ipEndPoint);
    }

    public void SendToServer(byte[] data)
    {
        UdpClientConnection udpClientConnection = udpConnection as UdpClientConnection;

        if (udpClientConnection != null)
            udpClientConnection.Send(data);
    }

    #endregion

    public void ReceiveData(byte[] data, IPEndPoint ipEndPoint)
    {
        if (!udpClientsIDs.ContainsKey(ipEndPoint))
            AddClient(ipEndPoint);

        if (OnReceiveData != null)
            OnReceiveData.Invoke(data, ipEndPoint);
    }
}