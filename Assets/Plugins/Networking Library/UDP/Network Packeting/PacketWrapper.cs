using System.IO;

public class PacketWrapper : ISerializablePacket
{
    public byte[] PacketData { get; set; }
    public uint Crc { get; set; }

    public void Serialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(Crc);
        binaryWriter.Write(PacketData);
    }

    public void Deserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        Crc = binaryReader.ReadUInt32();
        PacketData = binaryReader.ReadBytes((int)(stream.Length - stream.Position));
    }
}