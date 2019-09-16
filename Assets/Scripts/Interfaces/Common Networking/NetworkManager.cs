using System;
using System.Net;

public enum ConnectionProtocol
{
    TCP, UDP
}

public abstract class NetworkManager : MonoBehaviourSingleton<NetworkManager>, IDataReceiver
{
    public static ConnectionProtocol ConnectionProtocol { get; set; } = ConnectionProtocol.UDP;
    
    public  Action<byte[], IPEndPoint> OnReceiveData;
    public bool IsServer { get; protected set; }

    public static new NetworkManager Instance
    {
        get
        {
            if (!instance)
            {
                if (ConnectionProtocol == ConnectionProtocol.TCP)
                    instance = TcpNetworkManager.Instance;
                else
                    instance = UdpNetworkManager.Instance;
            }

            return instance;
        }
    }

    public virtual void ReceiveData(byte[] data, IPEndPoint ipEndPoint = null)
    {
        if (OnReceiveData != null)
            OnReceiveData.Invoke(data, ipEndPoint);
    }

    public abstract void StartServer(int port);
    public abstract void StartClient(IPAddress serverIP, int port);
    public abstract void Broadcast(byte[] data);
    public abstract void SendToServer(byte[] data);
}