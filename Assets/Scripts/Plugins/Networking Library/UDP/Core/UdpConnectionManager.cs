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
    public long clientSalt;
    public long serverSalt;
}

public class UdpConnectionManager : MonoBehaviourSingleton<UdpConnectionManager>
{
    public uint ClientID { get; private set; }

    Dictionary<uint, UdpClientData> udpClientsData = new Dictionary<uint, UdpClientData>();
    Dictionary<IPEndPoint, uint> udpClientsIDs = new Dictionary<IPEndPoint, uint>();
    ClientConnectionState clientConnectionState = ClientConnectionState.Idle;
    long clientSalt;
    long challengeResult;

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
            
            case ClientConnectionState.Connected:
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
                    challengeResult = clientSalt ^ challengeRequestPacket.Payload.serverSalt;
                    clientConnectionState = ClientConnectionState.SendingChallengeResponse;
                }
                break;

            case PacketType.ConnectionAccepted:
                if (!UdpNetworkManager.Instance.IsServer && clientConnectionState == ClientConnectionState.SendingChallengeResponse)
                {
                    ConnectionAcceptedPacket connectionAcceptedPacket = new ConnectionAcceptedPacket();
                    
                    connectionAcceptedPacket.Deserialize(stream);
                    Debug.Log(connectionAcceptedPacket.Payload.welcomeMessage);
                    clientConnectionState = ClientConnectionState.Connected;
                }
                break;

            case PacketType.ConnectionRequest:
                if (UdpNetworkManager.Instance.IsServer)
                {
                    if (!udpClientsIDs.ContainsKey(ipEndPoint))
                    {
                        ConnectionRequestPacket connectionRequestPacket = new ConnectionRequestPacket();
                        
                        connectionRequestPacket.Deserialize(stream);
                        AddClient(ipEndPoint, connectionRequestPacket.Payload.clientSalt);
                    }

                    UdpClientData udpClientData = udpClientsData[udpClientsIDs[ipEndPoint]];

                    SendChallengeRequest(udpClientData);
                }
                break;

            case PacketType.ChallengeResponse:
                if (UdpNetworkManager.Instance.IsServer)
                {
                    if (udpClientsIDs.ContainsKey(ipEndPoint))
                    {
                        ChallengeResponsePacket challengeResponsePacket = new ChallengeResponsePacket();
                        UdpClientData udpClientData = udpClientsData[udpClientsIDs[ipEndPoint]];
                        
                        challengeResponsePacket.Deserialize(stream);
                        if (challengeResponsePacket.Payload.result == (udpClientData.clientSalt ^ udpClientData.serverSalt))
                            SendConnectionAccepted();
                    }
                }
                break;
        }
    }

    void SendConnectionRequest()
    {
        ConnectionRequestPacket connectionRequestPacket = new ConnectionRequestPacket();
        ConnectionRequestData connectionRequestData;

        connectionRequestData.clientSalt = clientSalt;
        connectionRequestPacket.Payload = connectionRequestData;

        PacketsManager.Instance.SendPacket(connectionRequestPacket);
    }

    void SendChallengeRequest(UdpClientData udpClientData)
    {
        ChallengeRequestPacket challengeRequestPacket = new ChallengeRequestPacket();
        ChallengeRequestData challengeRequestData;

        challengeRequestData.generatedClientID = udpClientData.id;
        challengeRequestData.serverSalt = udpClientData.serverSalt;

        PacketsManager.Instance.SendPacket(challengeRequestPacket);
    }

    void SendChallengeResponse()
    {
        ChallengeResponsePacket challengeResponsePacket = new ChallengeResponsePacket();
        ChallengeResponseData challengeResponseData;

        challengeResponseData.result = challengeResult;
        challengeResponsePacket.Payload = challengeResponseData;

        PacketsManager.Instance.SendPacket(challengeResponsePacket);
    }

    void SendConnectionAccepted()
    {
        ConnectionAcceptedPacket connectionAcceptedPacket = new ConnectionAcceptedPacket();
        ConnectionAcceptedData connectionAcceptedData;

        connectionAcceptedData.welcomeMessage = "You have been connected to the server successfuly.";
        connectionAcceptedPacket.Payload = connectionAcceptedData;

        PacketsManager.Instance.SendPacket(connectionAcceptedPacket);
    }

    void AddClient(IPEndPoint ipEndPoint, long clientSalt)
    {
        UdpClientData udpClientData;
        long serverSalt = GenerateSalt();

        udpClientData.ipEndPoint = ipEndPoint;
        udpClientData.id = ClientID;
        udpClientData.timestamp = Time.realtimeSinceStartup;
        udpClientData.clientSalt = clientSalt;
        udpClientData.serverSalt = serverSalt;

        udpClientsIDs.Add(ipEndPoint, ClientID);
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

    public void ConnectToServer(IPAddress ipAddress, int port)
    {
        UdpNetworkManager.Instance.StartClient(ipAddress, port);
        clientSalt = GenerateSalt();
        clientConnectionState = ClientConnectionState.RequestingConnection;
    }

    public void CreateServer(int port)
    {
        UdpNetworkManager.Instance.StartServer(port);
    }

    public List<IPEndPoint> ClientsIPs
    {
        get
        {
            List<IPEndPoint> clientIPs = new List<IPEndPoint>(udpClientsIDs.Keys);
            
            return clientIPs;
        }
    }
}