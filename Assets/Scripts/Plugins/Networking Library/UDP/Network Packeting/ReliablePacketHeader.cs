using System.IO;

public class ReliablePacketHeader : ISerializablePacket
{
    public int PacketID { get; set; }
    public int Acknowledge { get; set; }
    public int AckBits { get; set; }

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

        PacketID = binaryReader.ReadInt32();
        Acknowledge = binaryReader.ReadInt32();
        AckBits = binaryReader.ReadInt32();
    }
}