using System.IO;

public struct ConnectionAcceptedData
{
    public uint clientID;
    public uint clientsInSession;
}

public class ConnectionAcceptedPacket : NetworkPacket<ConnectionAcceptedData>
{
    public ConnectionAcceptedPacket() : base((ushort)PacketType.ConnectionAccepted)
    {
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.clientID);
        binaryWriter.Write(Payload.clientsInSession);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        ConnectionAcceptedData connectionAcceptedData;
        
        connectionAcceptedData.clientID = binaryReader.ReadUInt32();
        connectionAcceptedData.clientsInSession = binaryReader.ReadUInt32();
        Payload = connectionAcceptedData;
    }
}