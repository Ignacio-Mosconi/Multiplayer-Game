using System.IO;

public class PacketHeader : ISerializablePacket
{
    public ushort PacketTypeIndex { get; set; }
    public uint PacketID  { get; set; }
    public uint SenderID  { get; set; }
    public uint ObjectID  { get; set; }

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(PacketTypeIndex);
        binaryWriter.Write(PacketID);
        binaryWriter.Write(SenderID);
        binaryWriter.Write(ObjectID);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        PacketTypeIndex = binaryReader.ReadUInt16();
        PacketID = binaryReader.ReadUInt32();
        SenderID = binaryReader.ReadUInt32();
        ObjectID = binaryReader.ReadUInt32();
    }
}