using System;

public class ChatMessagesManager : MonoBehaviourSingleton<ChatMessagesManager>
{
    public string UserDisplayName { get; set; }

    const string OwnMessageOpeningTag = "<align=\"right\">";
    const string OuterMessageOpeningTag = "<align=\"left\">";
    const string DisplayNameOpeningTag = "<line-height=100%><b><line-indent=3%>";
    const string DisplayNameClosingTag = "</line-indent></b></align>";
    const string GeneralClosingTag = "</align>";
    const string EndOfChatEntryClosingTag = "<line-height=150%></align>";

    public void SendChatMessage(string message, uint senderID, uint objectID)
    {
        ChatMessagePacket chatMessagePacket = new ChatMessagePacket();
        ChatMessageData chatMessageData;

        chatMessageData.senderDisplayName = UserDisplayName;
        chatMessageData.message = message;
        chatMessagePacket.Payload = chatMessageData;
        PacketsManager.Instance.SendPacket(chatMessagePacket, null, senderID, objectID, reliable: true);
    }

    public void SendChatMessage(string senderDisplayName, string message, uint senderID, uint objectID)
    {
        ChatMessagePacket chatMessagePacket = new ChatMessagePacket();
        ChatMessageData chatMessageData;

        chatMessageData.senderDisplayName = senderDisplayName;
        chatMessageData.message = message;
        chatMessagePacket.Payload = chatMessageData;
        PacketsManager.Instance.SendPacket(chatMessagePacket, null, senderID, objectID, reliable: true);
    }

    public string FormatOwnDisplayName()
    {
        string displayName = OwnMessageOpeningTag + DisplayNameOpeningTag + UserDisplayName + 
                                DisplayNameClosingTag + GeneralClosingTag + Environment.NewLine;

        return displayName;
    }

    public string FormatOwnMessage(string message)
    {
        string formattedMessage = OwnMessageOpeningTag + message + 
                                    GeneralClosingTag + EndOfChatEntryClosingTag + Environment.NewLine;

        return formattedMessage;
    }

    public string FormatOuterDisplayName(string displayName)
    {
        string formattedDisplayName = OuterMessageOpeningTag + DisplayNameOpeningTag + displayName + 
                                        DisplayNameClosingTag + GeneralClosingTag + Environment.NewLine;

        return formattedDisplayName;
    }

    public string FormatOuterMessage(string message)
    {
        string formattedMessage = OuterMessageOpeningTag + message + 
                                    GeneralClosingTag + EndOfChatEntryClosingTag + Environment.NewLine;

        return formattedMessage;
    }
}