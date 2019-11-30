using System.IO;

public struct InputData
{
    public float[] movement;
}

public class InputPacket : UserNetworkPacket<InputData>
{
    public InputPacket() : base((ushort)UserPacketType.Input)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);

        for (int i = 0; i < Payload.movement.Length; i++)
            binaryWriter.Write(Payload.movement[i]);
    }

    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        InputData inputData;

        inputData.movement = new float[3];

        for (int i = 0; i < inputData.movement.Length; i++)
            inputData.movement[i] = binaryReader.ReadSingle();

        Payload = inputData;
    }
}