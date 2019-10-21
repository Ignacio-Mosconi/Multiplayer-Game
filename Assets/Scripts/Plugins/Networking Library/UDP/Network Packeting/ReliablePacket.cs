using System.IO;

public class ReliablePacket : ISerializablePacket
{
    public uint PacketID { get; set; }
    public uint Acknowledge { get; set; }
    public uint AckBits { get; set; }
    public byte[] PacketData { get; set; }

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(PacketID);
        binaryWriter.Write(Acknowledge);
        binaryWriter.Write(AckBits);
        binaryWriter.Write(PacketData.Length);
        binaryWriter.Write(PacketData);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        PacketID = binaryReader.ReadUInt32();
        Acknowledge = binaryReader.ReadUInt32();
        AckBits = binaryReader.ReadUInt32();
        
        int byteCount = binaryReader.ReadInt32();
        PacketData = binaryReader.ReadBytes(byteCount);
    }
}