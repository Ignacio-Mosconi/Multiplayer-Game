using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Net;

public class PacketsManager : MonoBehaviourSingleton<PacketsManager>, IDataReceiver
{
    Dictionary<uint, Action<ushort, uint, Stream>> packetReceptionCallbacks = new Dictionary<uint, Action<ushort, uint, Stream>>();
    Action<ushort, IPEndPoint, Stream> systemPacketReceptionCallback;
    CrcCalculator crcCalculator;

    const ushort ProtocolID = 0;
    const uint CrcPolynomialDivisor = 0x04C11DB7;

    void Start()
    {
        crcCalculator = new CrcCalculator(CrcPolynomialDivisor);
        UdpNetworkManager.Instance.OnReceiveData += ReceiveData;
        PacketReliabilityManager.Instance.SetUpReliabilitySystem();
    }

    void InvokeReceptionCallback(uint objectID, ushort userPacketTypeIndex, uint senderID, Stream stream)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks[objectID].Invoke(userPacketTypeIndex, senderID, stream);
    }

    bool DeserializePacket(byte[] data, out MemoryStream memoryStream, out PacketHeader packetHeader,
                                ref UserPacketHeader userPacketHeader, ref ReliablePacketHeader reliablePacketHeader)
    {
        bool isFaultless;
        
        PacketWrapper packetWrapper = new PacketWrapper();
        
        memoryStream = new MemoryStream(data);
        packetHeader = new PacketHeader();

        packetWrapper.Deserialize(memoryStream);

        memoryStream.Close();

        isFaultless = crcCalculator.PerformCrcCheck(packetWrapper.PacketData, packetWrapper.Crc);

        if (isFaultless)
        {
            memoryStream = new MemoryStream(packetWrapper.PacketData);

            packetHeader.Deserialize(memoryStream);

            if ((PacketType)packetHeader.PacketTypeIndex == PacketType.User)
            {
                userPacketHeader = new UserPacketHeader();
                userPacketHeader.Deserialize(memoryStream);

                if (userPacketHeader.Reliable)
                {
                    reliablePacketHeader = new ReliablePacketHeader();
                    reliablePacketHeader.Deserialize(memoryStream);
                }
            }
        }

        return isFaultless;
    }

    public byte[] SerializePacket<T>(NetworkPacket<T> networkPacket, uint senderID = 0, uint objectID = 0, 
                                    ReliablePacketHeader reliablePacketHeader = null)
    {
        byte[] data = null;

        PacketWrapper packetWrapper = new PacketWrapper();
        MemoryStream memoryStream = new MemoryStream();
        PacketHeader packetHeader = new PacketHeader();
        
        packetHeader.ProtocolID = ProtocolID;
        packetHeader.PacketTypeIndex = networkPacket.PacketTypeIndex;

        packetHeader.Serialize(memoryStream);

        if ((PacketType)networkPacket.PacketTypeIndex == PacketType.User)
        {
            UserNetworkPacket<T> userNetworkPacket = networkPacket as UserNetworkPacket<T>;
            UserPacketHeader userPacketHeader = new UserPacketHeader();
            
            userPacketHeader.UserPacketTypeIndex = userNetworkPacket.UserPacketTypeIndex;
            userPacketHeader.SenderID = senderID;
            userPacketHeader.ObjectID = objectID;
            userPacketHeader.Reliable = (reliablePacketHeader != null);
            
            userPacketHeader.Serialize(memoryStream);

            if (reliablePacketHeader != null)
                reliablePacketHeader.Serialize(memoryStream);
        }

        networkPacket.Serialize(memoryStream);
        memoryStream.Close();

        data = memoryStream.ToArray();
        
        packetWrapper.Crc = crcCalculator.ComputeCrc32(data);
        packetWrapper.PacketData = data;

        memoryStream = new MemoryStream();

        packetWrapper.Serialize(memoryStream);
        memoryStream.Close();
        
        data = memoryStream.ToArray();

        return data;
    }

    public void AddSystemPacketListener(Action<ushort, IPEndPoint, Stream> receptionCallback)
    {
        systemPacketReceptionCallback = receptionCallback;
    }

    public void AddUserPacketListener(uint objectID, Action<ushort, uint, Stream> receptionCallback)
    {
        if (!packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Add(objectID, receptionCallback);
    }

    Action<ushort, uint, Stream> GetPacketReceptionCallback(uint objectID)
    {
        Action<ushort, uint, Stream> processCallback = null;

        if (packetReceptionCallbacks.ContainsKey(objectID))
            processCallback = packetReceptionCallbacks[objectID];

        return processCallback;
    }

    public void RemoveUserPacketListener(uint objectID)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Remove(objectID);
    }

    public void SendPacket<T>(NetworkPacket<T> networkPacket, IPEndPoint ipEndPoint = null, uint senderID = 0, 
                                uint objectID = 0, bool reliable = false)
    {
        if (reliable)
            PacketReliabilityManager.Instance.SendPacket(networkPacket, senderID, objectID);
        else
        {
            byte[] data = SerializePacket(networkPacket, senderID, objectID);

            if (UdpNetworkManager.Instance.IsServer)
            {
                if (ipEndPoint != null)
                    UdpNetworkManager.Instance.SendToClient(data, ipEndPoint);
                else
                    UdpNetworkManager.Instance.Broadcast(data);
            }
            else
                UdpNetworkManager.Instance.SendToServer(data); 
        }
    }

    public void ReceiveData(byte[] data, IPEndPoint ipEndPoint)
    {
        MemoryStream memoryStream = null;
        PacketHeader packetHeader = null;
        UserPacketHeader userPacketHeader = null;
        ReliablePacketHeader reliablePacketHeader = null;

        if (DeserializePacket(data, out memoryStream, out packetHeader, ref userPacketHeader, ref reliablePacketHeader))
        {
            if (packetHeader.ProtocolID == ProtocolID)
            {
                if (userPacketHeader != null)
                {
                    if (userPacketHeader.Reliable)
                    {
                        Action<ushort, uint, Stream> processCallback = GetPacketReceptionCallback(userPacketHeader.ObjectID);
                        PacketReliabilityManager.Instance.ProcessReceivedStream(memoryStream, 
                                                                                userPacketHeader, 
                                                                                reliablePacketHeader, 
                                                                                processCallback);
                    }
                    else
                        InvokeReceptionCallback(userPacketHeader.ObjectID, userPacketHeader.UserPacketTypeIndex,
                                                userPacketHeader.SenderID, memoryStream);
                }
                else
                    systemPacketReceptionCallback?.Invoke(packetHeader.PacketTypeIndex, ipEndPoint, memoryStream);

                if (userPacketHeader == null || !userPacketHeader.Reliable)
                    memoryStream.Close();
            }
        }  
    }
}