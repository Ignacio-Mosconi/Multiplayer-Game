using System;
using System.Net;

public enum ConnectionProtocol
{
    UDP, TCP
}

public abstract class NetworkManager : MonoBehaviourSingleton<NetworkManager>, IDataReceiver
{
    public static ConnectionProtocol ConnectionProtocol { get; set; } = ConnectionProtocol.UDP;
    
    public Action<byte[], IPEndPoint> OnReceiveData;
    public bool IsServer { get; protected set; }

    public virtual void ReceiveData(byte[] data, IPEndPoint ipEndPoint = null)
    {
        if (OnReceiveData != null)
            OnReceiveData.Invoke(data, ipEndPoint);
    }

    public abstract void StartServer(int port);
    public abstract void StartClient(IPAddress serverIP, int port);
}