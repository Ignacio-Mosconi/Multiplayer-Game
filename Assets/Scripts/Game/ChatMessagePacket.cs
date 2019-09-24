using System.IO;

public class ChatMessagePacket : UserNetworkPacket<string>
{
    public ChatMessagePacket() : base(UserPacketType.ChatMessage)
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