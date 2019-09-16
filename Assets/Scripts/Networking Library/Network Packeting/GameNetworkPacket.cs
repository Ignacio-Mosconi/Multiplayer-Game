public enum PacketType
{
    Message
}

public abstract class GameNetworkPacket<T> : NetworkPacket<T>
{
    public GameNetworkPacket(PacketType packetType) : base((ushort)packetType)
    {

    }
}