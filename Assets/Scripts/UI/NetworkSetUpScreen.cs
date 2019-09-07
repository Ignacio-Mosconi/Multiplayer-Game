using System.Net;
using UnityEngine;
using TMPro;

public enum ConnectionProtocol
{
    TCP, UDP
}

public class NetworkSetUpScreen : MonoBehaviourSingleton<NetworkSetUpScreen>
{
    [SerializeField] ConnectionProtocol connectionProtocol = ConnectionProtocol.UDP;

    [SerializeField] TMP_InputField addressInputField = default;
    [SerializeField] TMP_InputField portInputField = default;

    public NetworkManager NetworkManager { get; private set; }

    void Start()
    {
        SetUpNetworkProtocol();
    }

    void MoveToChatScreen()
    {
        ChatScreen.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    void SetUpNetworkProtocol()
    {
        if (connectionProtocol == ConnectionProtocol.TCP)
            NetworkManager = TcpNetworkManager.Instance;
        else
            NetworkManager = UdpNetworkManager.Instance;
    }

    public void StartServer()
    {
        int port = System.Convert.ToInt32(portInputField.text);

        NetworkManager.StartServer(port);  
        MoveToChatScreen();
    }

    public void ConnectToServer()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);
        
        NetworkManager.StartClient(ipAddress, port);
        MoveToChatScreen();
    }
}