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

    void Start()
    {
        chatText.text = "";
        NetworkManager.Instance.OnReceiveData += OnReceiveData;
        chatInputField.onEndEdit.AddListener(OnEndEditChatMessage);
    }

    void OnReceiveData(byte[] data, IPEndPoint ipEndPoint = null)
    {
        if (NetworkManager.Instance.IsServer)
            NetworkManager.Instance.Broadcast(data);

        chatText.text += System.Text.Encoding.UTF8.GetString(data, 0, data.Length) + System.Environment.NewLine;
    }

    void OnEndEditChatMessage(string chatMessage)
    {
        if (chatMessage != "")
        {
            if (NetworkManager.Instance.IsServer)
            {
                NetworkManager.Instance.Broadcast(System.Text.Encoding.UTF8.GetBytes(chatMessage));
                chatText.text += chatMessage + System.Environment.NewLine;
            }
            else
                NetworkManager.Instance.SendToServer(System.Text.Encoding.UTF8.GetBytes(chatMessage));

            chatInputField.ActivateInputField();
            chatInputField.Select();
            chatInputField.text = "";
        }
    }
}