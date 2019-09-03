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

    void Start()
    {
        TcpNetworkManager.Instance.OnReceiveData += OnReceiveData;

        chatText.text = "";
        chatInputField.onEndEdit.AddListener(OnEndEditChatMessage);
    }

    void OnReceiveData(byte[] data)
    {
        if (TcpNetworkManager.Instance.IsServer)
            TcpNetworkManager.Instance.Broadcast(data);

        chatText.text += System.Text.Encoding.UTF8.GetString(data, 0, data.Length) + System.Environment.NewLine;
    }

    void OnEndEditChatMessage(string chatMessage)
    {
        if (chatMessage != "")
        {
            if (TcpNetworkManager.Instance.IsServer)
            {
                TcpNetworkManager.Instance.Broadcast(System.Text.Encoding.UTF8.GetBytes(chatMessage));
                chatText.text += chatMessage + System.Environment.NewLine;
            }
            else
                TcpNetworkManager.Instance.SendMessageToServer(System.Text.Encoding.UTF8.GetBytes(chatMessage));

            chatInputField.ActivateInputField();
            chatInputField.Select();
            chatInputField.text = "";
        }
    }
}