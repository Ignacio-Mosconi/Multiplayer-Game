using System.Net;
using UnityEngine;
using TMPro;

namespace SpaceshipGame
{
    public class NetworkSetUpScreen : MonoBehaviourSingleton<NetworkSetUpScreen>
    {
        [SerializeField] TMP_InputField addressInputField = default;
        [SerializeField] TMP_InputField portInputField = default;
        [SerializeField] TMP_InputField displayNameInputField = default;

        void Start()
        {
            NetworkManager.ConnectionProtocol = ConnectionProtocol.UDP;
        }

        void StartGame(uint clientsInSession = 0)
        {
            gameObject.SetActive(false);
            GameManager.Instance.StartGame(clientsInSession);
        }

        public void StartServer()
        {
            int port = System.Convert.ToInt32(portInputField.text);

            UdpConnectionManager.Instance.CreateServer(port);

            StartGame();
        }

        public void ConnectToServer()
        {
            if (UdpConnectionManager.Instance.ClientsIDs.Count >= GameManager.PlayerCount)
            {
                Debug.Log("The server is full.");
                return;
            }

            IPAddress ipAddress = IPAddress.Parse(addressInputField.text);
            int port = System.Convert.ToInt32(portInputField.text);
            
            UdpConnectionManager.Instance.ConnectToServer(ipAddress, port, StartGame);
        }
    }
}