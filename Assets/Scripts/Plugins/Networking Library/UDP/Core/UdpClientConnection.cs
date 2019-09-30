using System.Net;
using System.Net.Sockets;

public class UdpClientConnection : UdpConnection
{
    public UdpClientConnection(IPAddress ipAddress, int port, IDataReceiver dataReceiver = null) : base(dataReceiver)
    {
        connection = new UdpClient();
        connection.Connect(ipAddress, port);
        BeginDataReception();
    }

    public void Send(byte[] data)
    {
        connection.Send(data, data.Length);
    }
}