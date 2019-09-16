using System.IO;

public class ChatMessagePacket : GameNetworkPacket<string>
{
    public ChatMessagePacket() : base(PacketType.Message)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        binaryWriter.Write(Payload);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        Payload = binaryReader.ReadString();
    }
}