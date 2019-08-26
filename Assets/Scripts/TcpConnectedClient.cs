using System;
using System.Net.Sockets;

public class TcpConnectedClient
{
    TcpClient tcpClient;
    IDataReceiver dataReceiver;
    byte[] readBuffer = new byte[5000];

    public TcpConnectedClient(TcpClient tcpClient, IDataReceiver dataReceiver = null)
    {
        this.tcpClient = tcpClient;
        this.dataReceiver = dataReceiver;

        if (TcpNetworkManager.Instance.IsServer)
            Stream.BeginRead(readBuffer, 0, readBuffer.Length, OnRead, null);
    }

    void OnRead(IAsyncResult asyncResult)
    {
        int length = Stream.EndRead(asyncResult);
        if (length <= 0)
        {
            TcpNetworkManager.Instance.OnClientDisconnect(this);
            return;
        }

        byte[] data = readBuffer;

        dataReceiver.ReceiveData(data);
        Stream.BeginRead(readBuffer, 0, readBuffer.Length, OnRead, null);
    }

    public void Send(byte[] data)
    {
        Stream.Write(data, 0, data.Length);
    }

    public void OnEndConnection(IAsyncResult asyncResult)
    {
        tcpClient.EndConnect(asyncResult);
        Stream.BeginRead(readBuffer, 0, readBuffer.Length, OnRead, null);
    }

    public void CloseClient()
    {
        tcpClient.Close();
    }

    public NetworkStream Stream
    {
        get { return tcpClient.GetStream(); }
    }
}