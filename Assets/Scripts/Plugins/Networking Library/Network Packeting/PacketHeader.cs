using System.IO;

enum PacketType
{
    ConnectionRequest,
    ChallengeRequest,
    ChallengeResponse,
    ConnectionAccepted,
    User
}

public class PacketHeader : ISerializablePacket
{
    public ushort ProtocolID { get; set; } 
    public ushort PacketTypeIndex { get; set; }

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(ProtocolID);
        binaryWriter.Write(PacketTypeIndex);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        ProtocolID = binaryReader.ReadUInt16();
        PacketTypeIndex = binaryReader.ReadUInt16();
    }
}