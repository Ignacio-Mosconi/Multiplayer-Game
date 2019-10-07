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

    void OnUdpPacketReceived(ushort userPacketTypeIndex, Stream stream)
    {
        if (userPacketTypeIndex != (ushort)UserPacketType.ChatMessage)
            return;

        ChatMessagePacket chatMessagePacket = new ChatMessagePacket();

        chatMessagePacket.Deserialize(stream);

        if (UdpNetworkManager.Instance.IsServer)
            ChatMessagesManager.Instance.SendChatMessage(chatMessagePacket.Payload, 0);

        chatText.text += chatMessagePacket.Payload + Environment.NewLine;
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
            PacketsManager.Instance.AddUserPacketListener(0, OnUdpPacketReceived);
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
                if (UdpNetworkManager.Instance.IsServer)
                    chatText.text += chatMessage + Environment.NewLine;
                ChatMessagesManager.Instance.SendChatMessage(chatMessage, 0);
            }

            chatInputField.ActivateInputField();
            chatInputField.Select();
            chatInputField.text = "";
        }
    }
}