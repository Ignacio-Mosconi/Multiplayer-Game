using System;
using System.Text;
using System.IO;
using System.Net;
using UnityEngine;
using TMPro;

public class ChatScreen : MonoBehaviourSingleton<ChatScreen>
{
    [SerializeField] TextMeshProUGUI chatText = default;
    [SerializeField] TMP_InputField chatInputField = default;

    void OnUdpDataReceived(ushort userPacketTypeIndex, uint senderID, Stream stream)
    {
        if (userPacketTypeIndex != (ushort)UserPacketType.ChatMessage)
            return;

        ChatMessagePacket chatMessagePacket = new ChatMessagePacket();

        chatMessagePacket.Deserialize(stream);
        
        string senderDisplayName = chatMessagePacket.Payload.senderDisplayName;
        string message = chatMessagePacket.Payload.message;

        if (UdpNetworkManager.Instance.IsServer)
            ChatMessagesManager.Instance.SendChatMessage(senderDisplayName, message, senderID, 0);
        
        if (senderID != UdpNetworkManager.Instance.GetSenderID())
        {
            chatText.text += ChatMessagesManager.Instance.FormatOuterDisplayName(senderDisplayName);
            chatText.text += ChatMessagesManager.Instance.FormatOuterMessage(message);
        }
    }

    void OnTcpDataReceived(byte[] data, IPEndPoint ipEndPoint = null)
    {
        if (TcpNetworkManager.Instance.IsServer)
            TcpNetworkManager.Instance.Broadcast(data);

        chatText.text += System.Text.Encoding.UTF8.GetString(data, 0, data.Length) + Environment.NewLine;
    }

    public void Initialize()
    {
        chatText.text = "";

        if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            TcpNetworkManager.Instance.OnReceiveData += OnTcpDataReceived;
        else
            PacketsManager.Instance.AddUserPacketListener(0, OnUdpDataReceived);
    }

    public void OnEndEditChatMessage(string chatMessage)
    {
        if (chatMessage != "")
        {
            if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            {
                if (TcpNetworkManager.Instance.IsServer)
                {
                    TcpNetworkManager.Instance.Broadcast(Encoding.UTF8.GetBytes(chatMessage));
                    chatText.text += chatMessage + Environment.NewLine;
                }
                else
                    TcpNetworkManager.Instance.SendToServer(Encoding.UTF8.GetBytes(chatMessage));
            }
            else
            {
                chatText.text += ChatMessagesManager.Instance.FormatOwnDisplayName();
                chatText.text += ChatMessagesManager.Instance.FormatOwnMessage(chatMessage);
                ChatMessagesManager.Instance.SendChatMessage(chatMessage, UdpNetworkManager.Instance.GetSenderID(), 0);
            }

            chatInputField.ActivateInputField();
            chatInputField.Select();
            chatInputField.text = "";
        }
    }
}