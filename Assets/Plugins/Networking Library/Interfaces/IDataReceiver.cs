using System.Net;

public interface IDataReceiver
{
    void ReceiveData(byte[] data, IPEndPoint ipEndPoint = null);
}