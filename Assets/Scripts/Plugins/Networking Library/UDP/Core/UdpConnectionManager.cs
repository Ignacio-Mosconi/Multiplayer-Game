using System.Net;
using System.Collections.Generic;

public enum ConnectionState
{
    AwatingChallenge,
    AwatingResponse,
}

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

public class UdpConnectionManager : MonoBehaviourSingleton<UdpConnectionManager>
{
    public uint ClientID { get; private set; }

    Dictionary<uint, UdpClientData> udpClientsData = new Dictionary<uint, UdpClientData>();
    Dictionary<IPEndPoint, uint> udpClientsIDs = new Dictionary<IPEndPoint, uint>();

    void Update()
    {

    }

    void SendConnectionRequest()
    {
        if (UdpNetworkManager.Instance.IsServer)
            return;
    }

    void SendChallengeRequest()
    {
        if (!UdpNetworkManager.Instance.IsServer)
            return;
    }

    void SendChallengeResponse()
    {
        if (UdpNetworkManager.Instance.IsServer)
            return;
    }

    void SendConnectionAccepeted()
    {
        if (!UdpNetworkManager.Instance.IsServer)
            return;
    }

    public void ConnectToServer(IPAddress ipAddress, int port)
    {
        UdpNetworkManager.Instance.StartClient(ipAddress, port);

    }

    // void AddClient(IPEndPoint ipEndPoint)
    // {
    //     UdpClientData udpClientData = new UdpClientData(ipEndPoint, ClientID, Time.realtimeSinceStartup);

    //     udpClientsIDs[ipEndPoint] = ClientID;
    //     udpClientsData.Add(ClientID, udpClientData);

    //     ClientID++;
    // }

    // void RemoveClient(IPEndPoint ipEndPoint)
    // {
    //     if (!udpClientsIDs.ContainsKey(ipEndPoint))
    //     {
    //         Debug.LogWarning("Cannot remove the client because there are none with the given IP End Point.", gameObject);
    //         return;
    //     }

    //     udpClientsIDs.Remove(ipEndPoint);
    // }

    public List<IPEndPoint> ClientsIPs
    {
        get
        {
            List<IPEndPoint> clientIPs = new List<IPEndPoint>(udpClientsIDs.Keys);
            
            return clientIPs;
        }
    }
}