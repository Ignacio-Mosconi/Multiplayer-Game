using System;
using System.Collections.Generic;
using System.Net.Sockets;

public class TcpConnectedClient
{
    TcpClient tcpClient;
    IDataReceiver dataReceiver;
    Queue<byte[]> dataReceived = new Queue<byte[]>();
    byte[] readBuffer = new byte[5000];
    object handler = new object();

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

        lock (handler)
        {
            List<byte> dataList = new List<byte>();
            int i = 0;
            
            while ((char)readBuffer[i] != '\0')
            {
                dataList.Add(readBuffer[i]);
                i++;
            }
            
            byte[] data = new byte[dataList.Count];
            dataList.CopyTo(data);

            dataReceived.Enqueue(data);
        }
        
        Array.Clear(readBuffer, 0, readBuffer.Length);
        Stream.BeginRead(readBuffer, 0, readBuffer.Length, OnRead, null);
    }

    public void Send(byte[] data)
    {
        Stream.Write(data, 0, data.Length);
    }

    public void FlushReceivedData()
    {
        lock (handler)
            while (dataReceived.Count > 0)
            {
                byte[] data = dataReceived.Dequeue();

                if (dataReceiver != null)
                    dataReceiver.ReceiveData(data);
            }
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