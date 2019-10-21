using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public struct ReliablePacketData
{
    public IPEndPoint ipEndPoint;
    public byte[] packetData;
}

public class PacketReliabilityManager : MonoBehaviourSingleton<PacketReliabilityManager>
{
    Dictionary<uint, ReliablePacketData> packetDataPendingAck = new Dictionary<uint, ReliablePacketData>();
    Dictionary<uint, ReliablePacket> packetsWaitingPreviousAck = new Dictionary<uint, ReliablePacket>();
    Dictionary<uint, Action<ushort, uint, Stream>> packetReceptionCallbacks = new Dictionary<uint, Action<ushort, uint, Stream>>();
    List<uint> receivedPacketIDs = new List<uint>(sizeof(uint));
    uint currentPacketID = 0;
    uint nextExpectedID = 0;
    uint acknowledge = 0;
    uint ackBits = 0;

    void Update()
    {

    }

    byte[] SerializeReliablePacket(byte[] data, out uint packetID)
    {
        byte[] packetData = null;
        ReliablePacket reliablePacket = new ReliablePacket();
        MemoryStream memoryStream = new MemoryStream();

        reliablePacket.PacketID = currentPacketID++;
        reliablePacket.Acknowledge = acknowledge;
        reliablePacket.AckBits = ackBits;
        reliablePacket.PacketData = data;

        packetID = reliablePacket.PacketID;

        reliablePacket.Serialize(memoryStream);
        memoryStream.Close();
        packetData = memoryStream.ToArray();

        return packetData;
    }

    ReliablePacket DeserializeReliablePacket(Stream stream)
    {
        ReliablePacket reliablePacket = new ReliablePacket();
        
        reliablePacket.Deserialize(stream);

        return reliablePacket;
    }

    void SendDataToDestination(byte[] dataToSend, IPEndPoint ipEndPoint)
    {
        if (UdpNetworkManager.Instance.IsServer)
        {
            if (ipEndPoint != null)
                UdpNetworkManager.Instance.SendToClient(dataToSend, ipEndPoint);
            else
                UdpNetworkManager.Instance.Broadcast(dataToSend);
        }
        else
            UdpNetworkManager.Instance.SendToServer(dataToSend);
    }

    void SendUnacknowledgedPacketData()
    {
        foreach (ReliablePacketData pendingReliablePacket in packetDataPendingAck.Values)
            SendDataToDestination(pendingReliablePacket.packetData, pendingReliablePacket.ipEndPoint);
    }

    void InvokeReceptionCallback(uint objectID, ushort userPacketTypeIndex, uint senderID, Stream stream)
    {
        if (packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks[objectID].Invoke(userPacketTypeIndex, senderID, stream);
    }

    public void AddUserPacketListener(uint objectID, Action<ushort, uint, Stream> receptionCallback)
    {
        if (!packetReceptionCallbacks.ContainsKey(objectID))
            packetReceptionCallbacks.Add(objectID, receptionCallback);
    }

    public void SendPacketData(byte[] data, IPEndPoint ipEndPoint = null, bool reliable = false)
    {
        byte[] dataToSend = data;

        if (reliable)
        {
            ReliablePacketData pendingReliablePacketData;
            uint packetID;

            dataToSend = SerializeReliablePacket(data, out packetID);
            pendingReliablePacketData.ipEndPoint = ipEndPoint;
            pendingReliablePacketData.packetData = dataToSend;
            
            packetDataPendingAck.Add(packetID, pendingReliablePacketData);
        }

        SendDataToDestination(dataToSend, ipEndPoint);
    }

    public void ProcessReceivedStream(Stream stream, PacketHeader packetHeader, UserPacketHeader userPacketHeader)
    {
        if (userPacketHeader.Reliable)
        {
            ReliablePacket reliablePacket = DeserializeReliablePacket(stream);

            acknowledge = reliablePacket.PacketID;
            ackBits = 0;

            for (int i = 0; i < Marshal.SizeOf(ackBits); i++)
                if (receivedPacketIDs.Contains((uint)(acknowledge - 1 - i)))
                    ackBits += (uint)Math.Pow(2, i);

            if (receivedPacketIDs.Count == Marshal.SizeOf(ackBits))
                receivedPacketIDs.RemoveAt(receivedPacketIDs.Count - 1);

            receivedPacketIDs.Insert(0, acknowledge);
            receivedPacketIDs.Sort((a, b) => -a.CompareTo(b));

            if (reliablePacket.Acknowledge >= nextExpectedID)
            {
                if (reliablePacket.Acknowledge == nextExpectedID)
                {                    
                    char[] binaryRepresentation = Convert.ToString(reliablePacket.AckBits, 2).ToCharArray();

                    for (int i = 0; i < binaryRepresentation.Length; i++)
                        if (binaryRepresentation[binaryRepresentation.Length - 1 - i] == '1')
                        {
                            uint packetID = (uint)(nextExpectedID - 1 - i);

                            if (packetDataPendingAck.ContainsKey(packetID))
                                packetDataPendingAck.Remove(packetID);
                        }

                    packetDataPendingAck.Remove(nextExpectedID);
                    nextExpectedID++;
                }
                else
                    packetsWaitingPreviousAck.Add(reliablePacket.Acknowledge, reliablePacket);

                SendUnacknowledgedPacketData();
            }
        }
        else
            InvokeReceptionCallback(userPacketHeader.ObjectID, userPacketHeader.UserPacketTypeIndex, userPacketHeader.SenderID, stream);
    }
}