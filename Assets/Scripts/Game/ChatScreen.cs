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

    protected override void Awake()
    {
        base.Awake();
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            NetworkManager.Instance.OnReceiveData += OnTcpDataReceived;
        else
            PacketsManager.Instance.AddPacketListener(0, OnUdpPacketReceived);
    }

    void OnDisable()
    {
        if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            NetworkManager.Instance.OnReceiveData -= OnTcpDataReceived;            
        else
            PacketsManager.Instance.RemovePacketListener(0);
    }

    void Start()
    {
        chatText.text = "";
        chatInputField.onEndEdit.AddListener(OnEndEditChatMessage);
    }

    void OnUdpPacketReceived(ushort packetTypeIndex, Stream stream)
    {
        if (packetTypeIndex != (ushort)PacketType.Message)
            return;

        ChatMessagePacket chatMessagePacket = new ChatMessagePacket();

        chatMessagePacket.Deserialize(stream);

        if (UdpNetworkManager.Instance.IsServer)
            ChatMessagesManager.Instance.SendChatMessage(chatMessagePacket.Payload, 0);

        chatText.text += chatMessagePacket.Payload + Environment.NewLine;
    }

    void OnTcpDataReceived(byte[] data, IPEndPoint ipEndPoint = null)
    {
        if (NetworkManager.Instance.IsServer)
            NetworkManager.Instance.Broadcast(data);

        chatText.text += System.Text.Encoding.UTF8.GetString(data, 0, data.Length) + Environment.NewLine;
    }

    void OnEndEditChatMessage(string chatMessage)
    {
        if (chatMessage != "")
        {
            if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            {
                if (NetworkManager.Instance.IsServer)
                {
                    NetworkManager.Instance.Broadcast(Encoding.UTF8.GetBytes(chatMessage));
                    chatText.text += chatMessage + Environment.NewLine;
                }
                else
                    NetworkManager.Instance.SendToServer(Encoding.UTF8.GetBytes(chatMessage));
            }
            else
            {
                if (NetworkManager.Instance.IsServer)
                    chatText.text += chatMessage + Environment.NewLine;
                ChatMessagesManager.Instance.SendChatMessage(chatMessage, 0);
            }

            chatInputField.ActivateInputField();
            chatInputField.Select();
            chatInputField.text = "";
        }
    }
}