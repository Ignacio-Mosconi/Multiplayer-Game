using System.IO;

public struct ClientJoinedData
{
    public uint clientID;
}

public class ClientJoinedPacket : NetworkPacket<ClientJoinedData>
{
    public ClientJoinedPacket() : base((ushort)PacketType.ClientJoined)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(Payload.clientID);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        ClientJoinedData clientJoinedData;

        clientJoinedData.clientID = binaryReader.ReadUInt32();
        Payload = clientJoinedData;
    }
}