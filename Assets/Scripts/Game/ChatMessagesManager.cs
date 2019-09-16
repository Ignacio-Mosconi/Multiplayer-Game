public class ChatMessagesManager : MonoBehaviourSingleton<ChatMessagesManager>
{
    public void SendChatMessage(string message, uint objectID)
    {
        ChatMessagePacket chatMessagePacket = new ChatMessagePacket();

        chatMessagePacket.Payload = message;
        PacketsManager.Instance.SendPacket(chatMessagePacket, objectID);
    }
}