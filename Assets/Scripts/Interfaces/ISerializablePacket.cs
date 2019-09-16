using System.IO;

public interface ISerializablePacket
{
    void Serialize(Stream stream);
    void Deserialize(Stream stream);
}