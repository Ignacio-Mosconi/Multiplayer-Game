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

    void MoveToChatScreen()
    {
        ChatScreen.Instance.AddReceptionCallback(connectionProtocol);
        ChatScreen.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    public void StartServer()
    {
        int port = System.Convert.ToInt32(portInputField.text);

        if (connectionProtocol == ConnectionProtocol.TCP)
            TcpNetworkManager.Instance.StartServer(port);
        else
            UdpNetworkManager.Instance.StartServer(port);
        
        MoveToChatScreen();
    }

    public void ConnectToServer()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        if (connectionProtocol == ConnectionProtocol.TCP)
            TcpNetworkManager.Instance.StartClient(ipAddress, port);
        else
            UdpNetworkManager.Instance.StartClient(ipAddress, port);

        MoveToChatScreen();
    }
}