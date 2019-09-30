using System.IO;

public abstract class NetworkPacket<T> : ISerializablePacket
{
    public T Payload { get; set; }
    public ushort PacketTypeIndex { get; private set; }

    public NetworkPacket(ushort packetTypeIndex)
    {
        PacketTypeIndex = packetTypeIndex;
    }

    protected abstract void OnSerialize(Stream stream);
    protected abstract void OnDeserialize(Stream stream);

    public virtual void Serialize(Stream stream)
    {
        OnSerialize(stream);
    }

    public virtual void Deserialize(Stream stream)
    {
        OnDeserialize(stream);
    }
}