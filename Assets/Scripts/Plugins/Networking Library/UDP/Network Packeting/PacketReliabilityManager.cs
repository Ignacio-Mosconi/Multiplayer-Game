using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

public struct PacketPendingAck
{
    public uint recipientID;
    public uint packetID;
    public byte[] packetData;
}

public struct PacketPendingProcess
{
    public ReliablePacketHeader reliablePacketHeader;
    public UserPacketHeader userPacketHeader;
    public Stream stream;
    public Action<ushort, uint, Stream> receptionCallback;
}

public class PacketReliabilityManager : MonoBehaviourSingleton<PacketReliabilityManager>
{
    Dictionary<uint, List<PacketPendingAck>> packetsPendingAck = new Dictionary<uint, List<PacketPendingAck>>();
    Dictionary<uint, List<PacketPendingProcess>> packetsPendingProcess = new Dictionary<uint, List<PacketPendingProcess>>();
    Dictionary<uint, List<uint>> receivedPacketIDs = new Dictionary<uint, List<uint>>();
    Dictionary<uint, uint> nextExpectedIDs = new Dictionary<uint, uint>();
    Dictionary<uint, uint> acknowledges = new Dictionary<uint, uint>();
    Dictionary<uint, uint> ackBits = new Dictionary<uint, uint>();
    uint currentPacketID = 0;

    const int AckBitsCount = sizeof(uint) * 8;

    void Update()
    {
        using (var dicIterator = packetsPendingAck.GetEnumerator())
            while (dicIterator.MoveNext())
                using (var listIterator = dicIterator.Current.Value.GetEnumerator())
                    while (listIterator.MoveNext())
                        SendDataToDestination(listIterator.Current.packetData, listIterator.Current.recipientID);
    }

    void AddClientEntry(uint clientID)
    {
        if (packetsPendingAck.ContainsKey(clientID))
            return;

        packetsPendingAck.Add(clientID, new List<PacketPendingAck>(AckBitsCount));
        packetsPendingProcess.Add(clientID, new List<PacketPendingProcess>(AckBitsCount));
        receivedPacketIDs.Add(clientID, new List<uint>(AckBitsCount));
        nextExpectedIDs.Add(clientID, 0);
        acknowledges.Add(clientID, 0);
        ackBits.Add(clientID, 0);
    }

    void SetUpClient()
    {
        packetsPendingAck.Add(0, new List<PacketPendingAck>(AckBitsCount));
        packetsPendingProcess.Add(0, new List<PacketPendingProcess>(AckBitsCount));
        receivedPacketIDs.Add(0, new List<uint>(AckBitsCount));
        nextExpectedIDs.Add(0, 0);
        acknowledges.Add(0, 0);
        ackBits.Add(0, 0);
    }

    void SendDataToDestination(byte[] dataToSend, uint recipientID = 0)
    {
        if (UdpNetworkManager.Instance.IsServer)
        {
            IPEndPoint ipEndPoint = UdpConnectionManager.Instance.GetClientIP(recipientID);
            UdpNetworkManager.Instance.SendToClient(dataToSend, ipEndPoint);
        }
        else
            UdpNetworkManager.Instance.SendToServer(dataToSend);
    }

    public void SetUpReliabilitySystem()
    {
        if (UdpNetworkManager.Instance.IsServer)
        {
            foreach (uint clientID in UdpConnectionManager.Instance.ClientsIDs)
                AddClientEntry(clientID);
            UdpConnectionManager.Instance.OnClientAddedByServer += AddClientEntry;
        }
        else
            SetUpClient();
    }

    public void SendPacket<T>(NetworkPacket<T> networkPacket, uint senderID, uint objectID)
    {
        uint packetID = currentPacketID++;

        foreach (uint recipientID in packetsPendingAck.Keys)
        {
            ReliablePacketHeader reliablePacketHeader = new ReliablePacketHeader();
            MemoryStream memoryStream = new MemoryStream();
            PacketPendingAck packetPendingAck;
            byte[] dataToSend;

            reliablePacketHeader.PacketID = packetID;
            reliablePacketHeader.Acknowledge = acknowledges[recipientID];
            reliablePacketHeader.AckBits = ackBits[recipientID];

            dataToSend = PacketsManager.Instance.SerializePacket(networkPacket, senderID, objectID, reliablePacketHeader); 

            packetPendingAck.recipientID = recipientID;
            packetPendingAck.packetID = packetID;
            packetPendingAck.packetData = dataToSend;

            packetsPendingAck[recipientID].Add(packetPendingAck);
            SendDataToDestination(dataToSend, recipientID);
        }
    }

    public void ProcessReceivedStream(Stream stream, UserPacketHeader userPacketHeader, 
                                        ReliablePacketHeader reliablePacketHeader, Action<ushort, uint, Stream> processCallback)
    {
        uint senderID = (UdpNetworkManager.Instance.IsServer) ? userPacketHeader.SenderID : 0;
        int i = 0;

        acknowledges[senderID] = reliablePacketHeader.PacketID;
        ackBits[senderID] = 0;

        for (i = AckBitsCount - 1; i >= 0; i--)
            if (receivedPacketIDs[senderID].Contains((uint)(acknowledges[senderID] - i - 1)))
                ackBits[senderID] |= (uint)(1 << i);

        receivedPacketIDs[senderID].Add(acknowledges[senderID]);
        if (receivedPacketIDs[senderID].Count > AckBitsCount)
            receivedPacketIDs[senderID].RemoveAt(0);

        if (reliablePacketHeader.PacketID >= nextExpectedIDs[senderID])
        {
            if (reliablePacketHeader.PacketID == nextExpectedIDs[senderID])
            {
                nextExpectedIDs[senderID] += (uint)packetsPendingProcess[senderID].Count + 1;

                processCallback(userPacketHeader.UserPacketTypeIndex, userPacketHeader.SenderID, stream);
                
                foreach (PacketPendingProcess packet in packetsPendingProcess[senderID])
                    packet.receptionCallback(packet.userPacketHeader.UserPacketTypeIndex, packet.userPacketHeader.SenderID, packet.stream);
                
                packetsPendingProcess[senderID].Clear();
            }
            else
            {
                PacketPendingProcess reliablePacketPendingProcess;

                reliablePacketPendingProcess.reliablePacketHeader = reliablePacketHeader;
                reliablePacketPendingProcess.userPacketHeader = userPacketHeader;
                reliablePacketPendingProcess.stream = stream;
                reliablePacketPendingProcess.receptionCallback = processCallback;

                packetsPendingProcess[senderID].Add(reliablePacketPendingProcess);
                if (packetsPendingProcess[senderID].Count > AckBitsCount)
                    packetsPendingProcess[senderID].RemoveAt(0);
            }
        }

        PacketPendingAck packetPendingAck;
        uint n = reliablePacketHeader.AckBits;
        
        i = 1;

        do
        {
            if ((n <<= 1 & 0) != 0)
            {
                uint packetID = (uint)(reliablePacketHeader.Acknowledge - i);

                packetPendingAck = packetsPendingAck[senderID].Find(ppa => ppa.packetID == packetID);
                
                if (packetPendingAck.packetData != null)
                {
                    packetsPendingAck[senderID].Remove(packetPendingAck);
                    packetPendingAck.packetData = null;
                }
            }
            i++;    
        } while (n != 0);

        packetPendingAck = packetsPendingAck[senderID].Find(ppa => ppa.packetID == reliablePacketHeader.Acknowledge);
        
        if (packetPendingAck.packetData != null)
            packetsPendingAck[senderID].Remove(packetPendingAck);
    }
}