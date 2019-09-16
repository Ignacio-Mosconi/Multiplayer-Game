using System.IO;

public class PacketHeader : ISerializablePacket
{
    public ushort PacketTypeIndex { get; set; }

    uint packetID;
    uint senderID;
    uint objectID;

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(PacketTypeIndex);
        binaryWriter.Write(packetID);
        binaryWriter.Write(senderID);
        binaryWriter.Write(objectID);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        PacketTypeIndex = binaryReader.ReadUInt16();
        packetID = binaryReader.ReadUInt32();
        senderID = binaryReader.ReadUInt32();
        objectID = binaryReader.ReadUInt32();
    }
}