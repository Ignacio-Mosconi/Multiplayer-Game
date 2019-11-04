using System.IO;

public struct ChallengeResponseData
{
    public long result;
}

public class ChallengeResponsePacket : NetworkPacket<ChallengeResponseData>
{
    public ChallengeResponsePacket() : base((ushort)PacketType.ChallengeResponse)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.result);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);
        ChallengeResponseData challengeResponseData;
        
        challengeResponseData.result = binaryReader.ReadInt64();
        Payload = challengeResponseData;
    }
}