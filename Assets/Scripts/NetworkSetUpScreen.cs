using System.Net;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkSetUpScreen : MonoBehaviourSingleton<NetworkSetUpScreen>
{
    [SerializeField] TMP_InputField addressInputField = default;
    [SerializeField] TMP_InputField portInputField = default;

    void MoveToChatScreen()
    {
        ChatScreen.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    public void StartServer()
    {
        int port = System.Convert.ToInt32(portInputField.text);
        TcpNetworkManager.Instance.StartServer(port);
        MoveToChatScreen();
    }

    public void ConnectToServer()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        TcpNetworkManager.Instance.StartClient(ipAddress, port);

        MoveToChatScreen();
    }
}