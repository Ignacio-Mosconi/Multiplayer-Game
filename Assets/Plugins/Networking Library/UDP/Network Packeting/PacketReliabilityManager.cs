using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;

public struct PacketPendingAck
{
    public uint recipientID;
    public int packetID;
    public byte[] packetData;

    public void Reset()
    {
        recipientID = default;
        packetID = default;
        packetData = default;
    }
}

public struct PacketPendingProcess
{
    public ReliablePacketHeader reliablePacketHeader;
    public UserPacketHeader userPacketHeader;
    public Stream stream;
    public Action<ushort, uint, Stream> receptionCallback;

    public void Reset()
    {
        reliablePacketHeader = default;
        userPacketHeader = default;
        stream = default;
        receptionCallback = default;
    }
}

public class PacketReliabilityManager : MonoBehaviourSingleton<PacketReliabilityManager>
{
    Dictionary<uint, PacketPendingAck[]> packetsPendingAck = new Dictionary<uint, PacketPendingAck[]>();
    Dictionary<uint, PacketPendingProcess[]> packetsPendingProcess = new Dictionary<uint, PacketPendingProcess[]>();
    Dictionary<uint, int[]> receivedPacketIDs = new Dictionary<uint, int[]>();
    Dictionary<uint, int> nextExpectedIDs = new Dictionary<uint, int>();
    Dictionary<uint, int> acknowledges = new Dictionary<uint, int>();
    Dictionary<uint, int> ackBits = new Dictionary<uint, int>();
    int currentPacketID = 0;

    const int AckBitsCount = sizeof(uint) * 8;

    void Update()
    {
        using (var dicIterator = packetsPendingAck.GetEnumerator())
            while (dicIterator.MoveNext())
                foreach (PacketPendingAck packetPendingAck in dicIterator.Current.Value)
                    if (packetPendingAck.packetData != null)
                        SendDataToDestination(packetPendingAck.packetData, packetPendingAck.recipientID);
    }

    void AddEntry(uint id = 0)
    {
        if (packetsPendingAck.ContainsKey(id))
            return;

        packetsPendingAck.Add(id, new PacketPendingAck[AckBitsCount]);
        packetsPendingProcess.Add(id, new PacketPendingProcess[AckBitsCount]);
        receivedPacketIDs.Add(id, new int[AckBitsCount]);
        nextExpectedIDs.Add(id, 0);
        acknowledges.Add(id, -Int32.MaxValue);
        ackBits.Add(id, 0);

        for (int i = 0; i < AckBitsCount; i++)
        {
            packetsPendingAck[id][i].Reset();
            packetsPendingProcess[id][i].Reset();
            receivedPacketIDs[id][i] = -Int32.MaxValue;
        }
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

    void UpdateAcknowledgeBits(uint senderID)
    {
        ackBits[senderID] = 0;

        for (int i = AckBitsCount - 1; i >= 0; i--)
            if (receivedPacketIDs[senderID].Contains(acknowledges[senderID] - i - 1))
                ackBits[senderID] |= 1 << i;
    }

    void ProcessReceivedAcknowledgeInfo(ReliablePacketHeader reliablePacketHeader, uint senderID)
    {
        int ackBits = reliablePacketHeader.AckBits;
        int pendingIndex = -1;
        int i = 1;

        do
        {
            if ((ackBits & 1) != 0)
            {
                pendingIndex = Array.FindIndex(packetsPendingAck[senderID], ppa => ppa.packetID == reliablePacketHeader.Acknowledge);

                if (pendingIndex != -1)
                    packetsPendingAck[senderID][pendingIndex].Reset();
            }
            ackBits >>= 1;
            i++;
        } while (ackBits != 0);

        pendingIndex = -1;
        pendingIndex = Array.FindIndex(packetsPendingAck[senderID], ppa => ppa.packetID == reliablePacketHeader.Acknowledge);

        if (pendingIndex != -1)
            packetsPendingAck[senderID][pendingIndex].Reset();
    }

    public void SetUpReliabilitySystem()
    {
        if (UdpNetworkManager.Instance.IsServer)
        {
            foreach (uint clientID in UdpConnectionManager.Instance.ClientsIDs)
                AddEntry(clientID);
            UdpConnectionManager.Instance.OnClientAddedByServer += AddEntry;
        }
        else
            AddEntry();
    }

    public void SendPacket<T>(NetworkPacket<T> networkPacket, uint senderID, uint objectID)
    {
        int packetID = currentPacketID++;

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

            packetsPendingAck[recipientID][packetID % AckBitsCount] = packetPendingAck;
            SendDataToDestination(dataToSend, recipientID);
        }
    }

    public void ProcessReceivedStream(Stream stream, UserPacketHeader userPacketHeader, ReliablePacketHeader reliablePacketHeader,
                                        Action<ushort, uint, Stream> processCallback)
    {
        if (UnityEngine.Random.Range(0, 100) < 99)
            return;

        uint senderID = (UdpNetworkManager.Instance.IsServer) ? userPacketHeader.SenderID : 0;
        
        if (reliablePacketHeader.PacketID >= nextExpectedIDs[senderID])
        {
            if (reliablePacketHeader.PacketID > acknowledges[senderID])
                acknowledges[senderID] = reliablePacketHeader.PacketID;
            receivedPacketIDs[senderID][reliablePacketHeader.PacketID % AckBitsCount] = reliablePacketHeader.PacketID;
            UpdateAcknowledgeBits(senderID);

            int packetIndex = reliablePacketHeader.PacketID % AckBitsCount;

            packetsPendingProcess[senderID][packetIndex].reliablePacketHeader = reliablePacketHeader;
            packetsPendingProcess[senderID][packetIndex].userPacketHeader = userPacketHeader;
            packetsPendingProcess[senderID][packetIndex].stream = stream;
            packetsPendingProcess[senderID][packetIndex].receptionCallback = processCallback;

            if (reliablePacketHeader.PacketID == nextExpectedIDs[senderID])
            {
                while (packetsPendingProcess[senderID][packetIndex].stream != null)
                {
                    processCallback(packetsPendingProcess[senderID][packetIndex].userPacketHeader.UserPacketTypeIndex,
                                    packetsPendingProcess[senderID][packetIndex].userPacketHeader.SenderID, 
                                    packetsPendingProcess[senderID][packetIndex].stream);
                    packetsPendingProcess[senderID][packetIndex].stream.Close();
                    packetsPendingProcess[senderID][packetIndex].Reset();
                    packetIndex = (packetIndex + 1) % AckBitsCount;
                    nextExpectedIDs[senderID]++;
                }
            }

            ProcessReceivedAcknowledgeInfo(reliablePacketHeader, senderID);
        }
    }
}