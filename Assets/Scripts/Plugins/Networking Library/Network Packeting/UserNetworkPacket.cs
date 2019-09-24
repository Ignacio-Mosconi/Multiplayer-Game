public enum UserPacketType
{
    ChatMessage
}

public abstract class UserNetworkPacket<T> : NetworkPacket<T>
{
    public ushort UserPacketTypeIndex { get; private set; }

    public UserNetworkPacket(UserPacketType userPacketType) : base((ushort)PacketType.User)
    {
        UserPacketTypeIndex = (ushort)userPacketType;
    }
}