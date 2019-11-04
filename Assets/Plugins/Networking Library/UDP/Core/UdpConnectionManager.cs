using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using UnityEngine;

public enum ClientConnectionState
{
    Idle,
    RequestingConnection,
    SendingChallengeResponse,
    Connected
}

public struct UdpClientData
{
    public IPEndPoint ipEndPoint;
    public uint id;
    public float timestamp;
}

public struct UdpPendingClientData
{
    public IPEndPoint ipEndPoint;
    public long clientSalt;
    public long serverSalt;
}

public class UdpConnectionManager : ConnectionManager
{
    public Action<uint> OnClientAddedByServer { get; set; }
    public uint ClientID { get; private set; } = 1;

    Dictionary<IPEndPoint, uint> udpClientsIDs = new Dictionary<IPEndPoint, uint>();
    Dictionary<IPEndPoint, UdpPendingClientData> udpPendingClientsData = new Dictionary<IPEndPoint, UdpPendingClientData>();
    Dictionary<uint, UdpClientData> udpClientsData = new Dictionary<uint, UdpClientData>();
    ClientConnectionState clientConnectionState = ClientConnectionState.Idle;
    long saltGeneratedByClient;
    long challengeResultGeneratedByClient;

    public static new UdpConnectionManager Instance
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<UdpConnectionManager>();
            if (!instance)
            {
                GameObject gameObject = new GameObject(typeof(UdpConnectionManager).Name);
                instance = gameObject.AddComponent<UdpConnectionManager>();
            }

            return instance as UdpConnectionManager;
        }
    }

    void Start()
    {
        PacketsManager.Instance.AddSystemPacketListener(ReceiveConnectionData);
    }

    void Update()
    {
        if (UdpNetworkManager.Instance.IsServer)
            return;

        switch (clientConnectionState)
        {
            case ClientConnectionState.RequestingConnection:
                SendConnectionRequest();
                break;

            case ClientConnectionState.SendingChallengeResponse:
                SendChallengeResponse();
                break;

            default:
                break;
        }
    }

    long GenerateSalt()
    {
        System.Random random = new System.Random();
        long salt = (long)(random.NextDouble() * Int64.MaxValue);

        return salt;
    }

    void ReceiveConnectionData(ushort packetTypeIndex, IPEndPoint ipEndPoint, Stream stream)
    {
        switch ((PacketType)packetTypeIndex)
        {
            case PacketType.ChallengeRequest:
                if (!UdpNetworkManager.Instance.IsServer && clientConnectionState == ClientConnectionState.RequestingConnection)
                {
                    ChallengeRequestPacket challengeRequestPacket = new ChallengeRequestPacket();
                    
                    challengeRequestPacket.Deserialize(stream);
                    
                    challengeResultGeneratedByClient = saltGeneratedByClient ^ challengeRequestPacket.Payload.serverSalt;
                    clientConnectionState = ClientConnectionState.SendingChallengeResponse;
                }
                break;

            case PacketType.ConnectionAccepted:
                if (!UdpNetworkManager.Instance.IsServer && clientConnectionState == ClientConnectionState.SendingChallengeResponse)
                {
                    ConnectionAcceptedPacket connectionAcceptedPacket = new ConnectionAcceptedPacket();
                    
                    connectionAcceptedPacket.Deserialize(stream);
                    UdpNetworkManager.Instance.SetClientID(connectionAcceptedPacket.Payload.clientID);
                    onClientConnectedCallback?.Invoke();
                    onClientConnectedCallback = null;
                    clientConnectionState = ClientConnectionState.Connected;
                }
                break;

            case PacketType.ConnectionRequest:
                if (UdpNetworkManager.Instance.IsServer && !udpClientsIDs.ContainsKey(ipEndPoint))
                {
                    if (!udpPendingClientsData.ContainsKey(ipEndPoint))
                    {
                        ConnectionRequestPacket connectionRequestPacket = new ConnectionRequestPacket();
                        
                        connectionRequestPacket.Deserialize(stream);
                        AddPendingClient(ipEndPoint, connectionRequestPacket.Payload.clientSalt);
                    }

                    UdpPendingClientData udpPendingClientData = udpPendingClientsData[ipEndPoint];

                    SendChallengeRequest(udpPendingClientData);
                }
                break;

            case PacketType.ChallengeResponse:
                if (UdpNetworkManager.Instance.IsServer)
                {
                    if (udpPendingClientsData.ContainsKey(ipEndPoint))
                    {
                        ChallengeResponsePacket challengeResponsePacket = new ChallengeResponsePacket();
                        UdpPendingClientData udpPendingClientData = udpPendingClientsData[ipEndPoint];
                        
                        challengeResponsePacket.Deserialize(stream);

                        long serverResult = udpPendingClientData.clientSalt ^ udpPendingClientData.serverSalt;

                        if (challengeResponsePacket.Payload.result == serverResult)
                        {
                            AddClient(ipEndPoint);
                            RemovePendingClient(ipEndPoint);
                        }
                    }
                    if (udpClientsIDs.ContainsKey(ipEndPoint))
                    {
                        SendConnectionAccepted(udpClientsData[udpClientsIDs[ipEndPoint]]);
                        OnClientAddedByServer?.Invoke(udpClientsIDs[ipEndPoint]);
                    }
                }
                break;
        }
    }

    void SendConnectionRequest()
    {
        ConnectionRequestPacket connectionRequestPacket = new ConnectionRequestPacket();
        ConnectionRequestData connectionRequestData;

        connectionRequestData.clientSalt = saltGeneratedByClient;
        connectionRequestPacket.Payload = connectionRequestData;

        PacketsManager.Instance.SendPacket(connectionRequestPacket);
    }

    void SendChallengeRequest(UdpPendingClientData udpPendingClientData)
    {
        ChallengeRequestPacket challengeRequestPacket = new ChallengeRequestPacket();
        ChallengeRequestData challengeRequestData;

        challengeRequestData.serverSalt = udpPendingClientData.serverSalt;
        challengeRequestPacket.Payload = challengeRequestData;

        PacketsManager.Instance.SendPacket(challengeRequestPacket, udpPendingClientData.ipEndPoint);
    }

    void SendChallengeResponse()
    {
        ChallengeResponsePacket challengeResponsePacket = new ChallengeResponsePacket();
        ChallengeResponseData challengeResponseData;

        challengeResponseData.result = challengeResultGeneratedByClient;
        challengeResponsePacket.Payload = challengeResponseData;

        PacketsManager.Instance.SendPacket(challengeResponsePacket);
    }

    void SendConnectionAccepted(UdpClientData udpClientData)
    {
        ConnectionAcceptedPacket connectionAcceptedPacket = new ConnectionAcceptedPacket();
        ConnectionAcceptedData connectionAcceptedData;

        connectionAcceptedData.clientID = udpClientData.id;
        connectionAcceptedPacket.Payload = connectionAcceptedData;

        PacketsManager.Instance.SendPacket(connectionAcceptedPacket, udpClientData.ipEndPoint);
    }

    void AddPendingClient(IPEndPoint ipEndPoint, long clientSalt)
    {
        UdpPendingClientData udpPendingClientData;
        long serverSalt = GenerateSalt();

        udpPendingClientData.ipEndPoint = ipEndPoint;
        udpPendingClientData.clientSalt = clientSalt;
        udpPendingClientData.serverSalt = serverSalt;

        udpPendingClientsData.Add(ipEndPoint, udpPendingClientData);
    }

    void AddClient(IPEndPoint ipEndPoint)
    {
        UdpClientData udpClientData;

        udpClientData.ipEndPoint = ipEndPoint;
        udpClientData.id = ClientID;
        udpClientData.timestamp = Time.realtimeSinceStartup;

        udpClientsIDs.Add(ipEndPoint, ClientID);
        udpClientsData.Add(ClientID, udpClientData);

        ClientID++;
    }

    void RemovePendingClient(IPEndPoint ipEndPoint)
    {
        if (!udpPendingClientsData.ContainsKey(ipEndPoint))
        {
            Debug.LogWarning("Cannot remove the pending client because there are none with the given IP End Point.", gameObject);
            return;
        }

        udpPendingClientsData.Remove(ipEndPoint);
    }

    void RemoveClient(IPEndPoint ipEndPoint)
    {
        if (!udpClientsIDs.ContainsKey(ipEndPoint))
        {
            Debug.LogWarning("Cannot remove the client because there are none with the given IP End Point.", gameObject);
            return;
        }

        udpClientsData.Remove(udpClientsIDs[ipEndPoint]);
        udpClientsIDs.Remove(ipEndPoint);
    }

    public override void CreateServer(int port)
    {
        UdpNetworkManager.Instance.StartServer(port);
    }

    public override void ConnectToServer(IPAddress ipAddress, int port, Action onClientConnectedCallback = null)
    {
        this.onClientConnectedCallback = onClientConnectedCallback;
        UdpNetworkManager.Instance.StartClient(ipAddress, port);
        saltGeneratedByClient = GenerateSalt();
        clientConnectionState = ClientConnectionState.RequestingConnection;
    }

    public IPEndPoint GetClientIP(uint id)
    {
        IPEndPoint ipEndPoint = null;

        if (udpClientsIDs.ContainsValue(id))
        {
            foreach (KeyValuePair<IPEndPoint, uint> pair in udpClientsIDs)
                if (EqualityComparer<uint>.Default.Equals(pair.Value, id))
                {
                    ipEndPoint = pair.Key;
                    break;
                }
        }
        else
            Debug.LogWarning("Attempted to get the IP of an inexistent client.", gameObject);

        return ipEndPoint;
    }

    public uint GetClientID(IPEndPoint ipEndPoint)
    {
        uint clientID = 0;

        if (udpClientsIDs.ContainsKey(ipEndPoint))
            clientID = udpClientsIDs[ipEndPoint];
        else
            Debug.LogWarning("Attempted to get the ID of a not-registered IP.", gameObject);

        return clientID;
    }

    public List<IPEndPoint> ClientsIPs
    {
        get
        {
            List<IPEndPoint> clientIPs = new List<IPEndPoint>(udpClientsIDs.Keys);
            
            return clientIPs;
        }
    }

    public List<uint> ClientsIDs
    {
        get
        {
            List<uint> clientIDs = new List<uint>(udpClientsIDs.Values);
            
            return clientIDs;
        }
    }
}