using System.IO;

public class UserPacketHeader : ISerializablePacket
{
    public ushort UserPacketTypeIndex { get; set; }
    public uint SenderID { get; set; }
    public uint ObjectID { get; set; }
    public bool Reliable {get ; set; }

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(UserPacketTypeIndex);
        binaryWriter.Write(SenderID);
        binaryWriter.Write(ObjectID);
        binaryWriter.Write(Reliable);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        UserPacketTypeIndex = binaryReader.ReadUInt16();
        SenderID = binaryReader.ReadUInt32();
        ObjectID = binaryReader.ReadUInt32();
        Reliable = binaryReader.ReadBoolean();
    }
}