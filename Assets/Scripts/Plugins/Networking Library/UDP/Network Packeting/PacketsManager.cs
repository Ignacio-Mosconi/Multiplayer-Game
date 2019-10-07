using System;
using System.IO;
using System.Collections.Generic;
using System.Net;

public class PacketsManager : MonoBehaviourSingleton<PacketsManager>, IDataReceiver
{
    Dictionary<uint, Action<ushort, uint, Stream>> packetReceptionCallbacks = new Dictionary<uint, Action<ushort, uint, Stream>>();
    Action<ushort, IPEndPoint, Stream> packetReceptionCallback;
    uint currentPacketID = 0;

    const ushort ProtocolID = 0;

    void Start()
    {
        UdpNetworkManager.Instance.OnReceiveData += ReceiveData;
    }

    byte[] SerializePacket<T>(NetworkPacket<T> networkPacket, uint senderID = 0, uint objectID = 0)
    {
        byte[] data = null;

        MemoryStream memoryStream = new MemoryStream();
        PacketHeader packetHeader = new PacketHeader();
        
        packetHeader.ProtocolID = ProtocolID;
        packetHeader.PacketTypeIndex = networkPacket.PacketTypeIndex;

        packetHeader.Serialize(memoryStream);

        if ((PacketType)networkPacket.PacketTypeIndex == PacketType.User)
        {
            UserNetworkPacket<T> userNetworkPacket = networkPacket as UserNetworkPacket<T>;
            UserPacketHeader userPacketHeader = new UserPacketHeader();
            
            userPacketHeader.PacketID = currentPacketID++;
            userPacketHeader.SenderID = senderID;
            userPacketHeader.ObjectID = objectID;
            userPacketHeader.UserPacketTypeIndex = userNetworkPacket.UserPacketTypeIndex;
            
            userPacketHeader.Serialize(memoryStream);
        }

        networkPacket.Serialize(memoryStream);

        memoryStream.Close();
        data = memoryStream.ToArray();

        return data;
    }

    void DeserializePacket(byte[] data, out MemoryStream memoryStream, out PacketHeader packetHeader, ref UserPacketHeader userPacketHeader)
    {
        memoryStream = new MemoryStream(data);
        packetHeader = new PacketHeader();
        
        packetHeader.Deserialize(memoryStream);

        if ((PacketType)packetHeader.PacketTypeIndex == PacketType.User)
        {
            userPacketHeader = new UserPacketHeader();
            userPacketHeader.Deserialize(memoryStream);
        }
    }

    void InvokeReceptionCallback(uint objectID, ushort userPacketTypeIndex, uint senderID, Stream stream)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks[objectID].Invoke(userPacketTypeIndex, senderID, stream);
    }

    public void AddSystemPacketListener(Action<ushort, IPEndPoint, Stream> receptionCallback)
    {
        packetReceptionCallback = receptionCallback;
    }

    public void AddUserPacketListener(uint objectID, Action<ushort, uint, Stream> receptionCallback)
    {
        if (!packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Add(objectID, receptionCallback);
    }

    public void RemoveSystemPacketListener()
    {
        packetReceptionCallback = null;
    }

    public void RemoveUserPacketListener(uint objectID)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Remove(objectID);
    }

    public void SendPacket<T>(NetworkPacket<T> networkPacket, IPEndPoint ipEndPoint = null, uint senderID = 0, uint objectID = 0)
    {
        byte[] data = SerializePacket<T>(networkPacket, senderID, objectID);

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

    public void ReceiveData(byte[] data, IPEndPoint ipEndPoint)
    {
        MemoryStream memoryStream = null;
        PacketHeader packetHeader = null;
        UserPacketHeader userPacketHeader = null;

        DeserializePacket(data, out memoryStream, out packetHeader, ref userPacketHeader);
        
        if (userPacketHeader != null)
            InvokeReceptionCallback(userPacketHeader.ObjectID, userPacketHeader.UserPacketTypeIndex, userPacketHeader.SenderID, memoryStream);

        packetReceptionCallback?.Invoke(packetHeader.PacketTypeIndex, ipEndPoint, memoryStream);

        memoryStream.Close();
    }
}