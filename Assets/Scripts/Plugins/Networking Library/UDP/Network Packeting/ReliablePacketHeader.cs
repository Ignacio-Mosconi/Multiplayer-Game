using System.IO;

public class ReliablePacketHeader : ISerializablePacket
{
    public uint PacketID { get; set; }
    public uint Acknowledge { get; set; }
    public uint AckBits { get; set; }

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(PacketID);
        binaryWriter.Write(Acknowledge);
        binaryWriter.Write(AckBits);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        PacketID = binaryReader.ReadUInt32();
        Acknowledge = binaryReader.ReadUInt32();
        AckBits = binaryReader.ReadUInt32();
    }
}