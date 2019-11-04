using System.IO;

public struct ConnectionRequestData
{
    public long clientSalt;
}

public class ConnectionRequestPacket : NetworkPacket<ConnectionRequestData>
{
    public ConnectionRequestPacket() : base((ushort)PacketType.ConnectionRequest)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.clientSalt);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        ConnectionRequestData connectionRequestData;
        
        connectionRequestData.clientSalt = binaryReader.ReadInt64();
        Payload = connectionRequestData;
    }
}