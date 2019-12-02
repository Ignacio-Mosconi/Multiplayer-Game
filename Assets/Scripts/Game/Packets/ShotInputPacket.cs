using System.IO;


public struct ShotInputData
{
    public float[] hitPosition;
}   

public class ShotInputPacket : UserNetworkPacket<ShotInputData>
{
    public ShotInputPacket() : base((ushort)UserPacketType.ShotInput)
    {

    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader =  new BinaryReader(stream);

        ShotInputData shotInputData;

        shotInputData.hitPosition = new float[3];

        for (int i = 0; i < shotInputData.hitPosition.Length; i++)
        {
            shotInputData.hitPosition[i] = binaryReader.ReadSingle();
        }

        Payload = shotInputData;
    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        for (int i = 0; i < Payload.hitPosition.Length; i++)
        {
            binaryWriter.Write(Payload.hitPosition[i]);           
        }
    }
}
