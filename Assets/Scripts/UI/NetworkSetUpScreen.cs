using System.Net;
using UnityEngine;
using TMPro;

public enum ConnectionProtocol
{
    TCP, UDP
}

public class NetworkSetUpScreen : MonoBehaviourSingleton<NetworkSetUpScreen>
{
    [SerializeField] TMP_InputField addressInputField = default;
    [SerializeField] TMP_InputField portInputField = default;
    [SerializeField] TMP_Dropdown protocolDropdown = default;

    ConnectionProtocol connectionProtocol;

    public NetworkManager NetworkManager { get; private set; }

    void Start()
    {
        connectionProtocol = (ConnectionProtocol)protocolDropdown.value;
        protocolDropdown.onValueChanged.AddListener(ChangeNetworkProtocol);
    }

    void SetUpNetworkProtocol()
    {
        if (connectionProtocol == ConnectionProtocol.TCP)
            NetworkManager = TcpNetworkManager.Instance;
        else
            NetworkManager = UdpNetworkManager.Instance;
    }

    void MoveToChatScreen()
    {
        ChatScreen.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    void ChangeNetworkProtocol(int value)
    {
        connectionProtocol = (ConnectionProtocol)value;
    }

    public void StartServer()
    {
        int port = System.Convert.ToInt32(portInputField.text);

        SetUpNetworkProtocol();
        NetworkManager.StartServer(port);  
        MoveToChatScreen();
    }

    public void ConnectToServer()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        SetUpNetworkProtocol();
        NetworkManager.StartClient(ipAddress, port);
        MoveToChatScreen();
    }
}