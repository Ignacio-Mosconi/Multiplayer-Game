public abstract class UserNetworkPacket<T> : NetworkPacket<T>
{
    public ushort UserPacketTypeIndex { get; private set; }

    public UserNetworkPacket(ushort userPacketTypeIndex) : base((ushort)PacketType.User)
    {
        UserPacketTypeIndex = userPacketTypeIndex;
    }
}