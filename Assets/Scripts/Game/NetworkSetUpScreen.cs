using System.Net;
using UnityEngine;
using TMPro;

public enum UserPacketType
{
    ChatMessage,
    Transform
}

public class NetworkSetUpScreen : MonoBehaviourSingleton<NetworkSetUpScreen>
{
    [SerializeField] TMP_InputField addressInputField = default;
    [SerializeField] TMP_InputField portInputField = default;
    [SerializeField] TMP_InputField displayNameInputField = default;
    [SerializeField] TMP_Dropdown protocolDropdown = default;

    ConnectionProtocol connectionProtocol;

    void Start()
    {
        ChatScreen.Instance.gameObject.SetActive(false);
        ChangeNetworkProtocol(protocolDropdown.value);
        protocolDropdown.onValueChanged.AddListener(ChangeNetworkProtocol);
    }

    void MoveToChatScreen()
    {
        gameObject.SetActive(false);
        ChatScreen.Instance.gameObject.SetActive(true);
        ChatScreen.Instance.Initialize();
    }

    void ChangeNetworkProtocol(int value)
    {
        connectionProtocol = (ConnectionProtocol)value;
        NetworkManager.ConnectionProtocol = connectionProtocol;
    }

    public void StartServer()
    {
        int port = System.Convert.ToInt32(portInputField.text);

        ChatMessagesManager.Instance.UserDisplayName = displayNameInputField.text;

        if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            TcpConnectionManager.Instance.CreateServer(port);
        else
            UdpConnectionManager.Instance.CreateServer(port);  
        
        MoveToChatScreen();
    }

    public void ConnectToServer()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        ChatMessagesManager.Instance.UserDisplayName = displayNameInputField.text;

        if (NetworkManager.ConnectionProtocol == ConnectionProtocol.TCP)
            TcpConnectionManager.Instance.ConnectToServer(ipAddress, port, MoveToChatScreen);
        else
            UdpConnectionManager.Instance.ConnectToServer(ipAddress, port, MoveToChatScreen);
    }
}