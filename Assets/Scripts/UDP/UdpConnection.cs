using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public struct UdpReceivedData
{
    public byte[] data;
    public IPEndPoint ipEndPoint;
}

public class UdpConnection
{
    protected UdpClient connection;
    
    Queue<UdpReceivedData> udpReceivedData = new Queue<UdpReceivedData>();
    IDataReceiver dataReceiver;
    object handler = new object();

    public UdpConnection(IDataReceiver dataReceiver)
    {
        this.dataReceiver = dataReceiver;
    }

    void OnReceive(IAsyncResult asyncResult)
    {
        try
        {
            UdpReceivedData receivedData = new UdpReceivedData();
            receivedData.data = connection.EndReceive(asyncResult, ref receivedData.ipEndPoint);

            lock (handler)
                udpReceivedData.Enqueue(receivedData);
        }
        catch (SocketException e)
        {
            UnityEngine.Debug.LogError("UdpConnection Error: " + e.Message);
        }

        connection.BeginReceive(OnReceive, null);
    }

    protected void BeginDataReception()
    {
        connection.BeginReceive(OnReceive, null);
    }

    public void Close()
    {
        connection.Close();
    }

    public void FlushReceivedData()
    {
        lock (handler)
            while (udpReceivedData.Count > 0)
            {
                UdpReceivedData receivedData = udpReceivedData.Dequeue();
                if (dataReceiver != null)
                    dataReceiver.ReceiveData(receivedData.data, receivedData.ipEndPoint);
            }
    }
}