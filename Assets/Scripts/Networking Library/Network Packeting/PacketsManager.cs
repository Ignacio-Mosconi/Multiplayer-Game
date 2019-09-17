using System;
using System.IO;
using System.Collections.Generic;
using System.Net;

public class PacketsManager : MonoBehaviourSingleton<PacketsManager>, IDataReceiver
{
    Dictionary<uint, Action<ushort, Stream>> packetReceptionCallbacks = new Dictionary<uint, Action<ushort, Stream>>();
    uint currentPacketID = 0;

    void Start()
    {
        UdpNetworkManager.Instance.OnReceiveData += ReceiveData;
    }

    byte[] SerializePacket<T>(NetworkPacket<T> networkPacket, uint objectID)
    {
        byte[] data = null;

        PacketHeader packetHeader = new PacketHeader();
        MemoryStream memoryStream = new MemoryStream();

        packetHeader.PacketID = currentPacketID++;
        packetHeader.SenderID = UdpNetworkManager.Instance.ClientID;
        packetHeader.ObjectID = objectID;
        packetHeader.PacketTypeIndex = networkPacket.PacketTypeIndex;

        packetHeader.Serialize(memoryStream);
        networkPacket.Serialize(memoryStream);

        memoryStream.Close();
        data = memoryStream.ToArray();

        return data;
    }

    void DeserializePacket(byte[] data, out PacketHeader packetHeader, out MemoryStream memoryStream)
    {
        packetHeader = new PacketHeader();
        memoryStream = new MemoryStream(data);

        packetHeader.Deserialize(memoryStream);
    }

    void InvokeReceptionCallback(uint objectID, ushort packetTypeIndex, Stream stream)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks[objectID].Invoke(packetTypeIndex, stream);
    }

    public void AddPacketListener(uint objectID, Action<ushort, Stream> receptionCallback)
    {
        if (!packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Add(objectID, receptionCallback);
    }

    public void RemovePacketListener(uint objectID)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Remove(objectID);
    }

    public void SendPacket<T>(NetworkPacket<T> networkPacket, uint objectID)
    {
        byte[] data = SerializePacket<T>(networkPacket, objectID);

        if (UdpNetworkManager.Instance.IsServer)
            UdpNetworkManager.Instance.Broadcast(data);
        else
            UdpNetworkManager.Instance.SendToServer(data);
    }

    public void ReceiveData(byte[] data, IPEndPoint ipEndPoint)
    {
        PacketHeader packetHeader;
        MemoryStream memoryStream;

        DeserializePacket(data, out packetHeader, out memoryStream);
        InvokeReceptionCallback(packetHeader.ObjectID, packetHeader.PacketTypeIndex, memoryStream);

        memoryStream.Close();
    }
}