using System.IO;

public struct ConnectionAcceptedData
{
    public string welcomeMessage;
}

public class ConnectionAcceptedPacket : NetworkPacket<ConnectionAcceptedData>
{
    public ConnectionAcceptedPacket() : base((ushort)PacketType.ConnectionAccepted)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.welcomeMessage);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        ConnectionAcceptedData connectionAcceptedData;
        
        connectionAcceptedData.welcomeMessage= binaryReader.ReadString();
        Payload = connectionAcceptedData;
    }
}