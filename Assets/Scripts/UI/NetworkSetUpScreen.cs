using System.Net;
using UnityEngine;
using TMPro;

public class NetworkSetUpScreen : MonoBehaviourSingleton<NetworkSetUpScreen>
{
    [SerializeField] TMP_InputField addressInputField = default;
    [SerializeField] TMP_InputField portInputField = default;
    [SerializeField] TMP_Dropdown protocolDropdown = default;

    ConnectionProtocol connectionProtocol;

    void Start()
    {
        ChangeNetworkProtocol(protocolDropdown.value);
        protocolDropdown.onValueChanged.AddListener(ChangeNetworkProtocol);
    }

    void MoveToChatScreen()
    {
        ChatScreen.Instance.gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    void ChangeNetworkProtocol(int value)
    {
        connectionProtocol = (ConnectionProtocol)value;
        NetworkManager.ConnectionProtocol = connectionProtocol;
    }

    public void StartServer()
    {
        int port = System.Convert.ToInt32(portInputField.text);

        NetworkManager.Instance.StartServer(port);  
        MoveToChatScreen();
    }

    public void ConnectToServer()
    {
        IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
        int port = System.Convert.ToInt32(portInputField.text);

        NetworkManager.Instance.StartClient(ipAddress, port);
        MoveToChatScreen();
    }
}