using System.Net;
using System.Net.Sockets;

public class UdpServerConnection : UdpConnection
{
    public UdpServerConnection(int port, IDataReceiver dataReceiver = null) : base(dataReceiver)
    {
        connection = new UdpClient(port);
        BeginDataReception();
    }

    public void Send(byte[] data, IPEndPoint ipEndpoint)
    {
        connection.Send(data, data.Length, ipEndpoint);
    }
}