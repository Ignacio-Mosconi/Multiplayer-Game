using System.IO;
        public enum PlayerStatus
        {
            Alive = 0,
            Dead = 1
        }
public struct NotificationData
{
     public uint playerStatus;
}
public class NotificationPacket : UserNetworkPacket<NotificationData>
{
    

    public NotificationPacket() : base((ushort)UserPacketType.Notification)
    {

    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader =  new BinaryReader(stream);

        NotificationData notificationData;
        
        uint status = (uint)PlayerStatus.Dead; 

        notificationData.playerStatus = status;
        
        Payload = notificationData;
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        binaryWriter.Write(Payload.playerStatus);
    }
}
