using System.IO;

public struct ChallengeRequestData
{
    public long serverSalt;
}

public class ChallengeRequestPacket : NetworkPacket<ChallengeRequestData>
{
    public ChallengeRequestPacket() : base((ushort)PacketType.ChallengeRequest)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.serverSalt);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        ChallengeRequestData challengeRequestData;
        
        challengeRequestData.serverSalt = binaryReader.ReadInt64();
        Payload = challengeRequestData;
    }
}