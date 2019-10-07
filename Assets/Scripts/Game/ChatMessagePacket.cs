using System.IO;

public struct ChatMessageData
{
    public string senderDisplayName;
    public string message;
}

public class ChatMessagePacket : UserNetworkPacket<ChatMessageData>
{
    public ChatMessagePacket() : base((ushort)UserPacketType.ChatMessage)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.senderDisplayName);
        binaryWriter.Write(Payload.message);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        ChatMessageData chatMessageData;
        
        chatMessageData.senderDisplayName = binaryReader.ReadString();
        chatMessageData.message = binaryReader.ReadString();

        Payload = chatMessageData;
    }
}