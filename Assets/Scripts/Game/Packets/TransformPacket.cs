using System.IO;

public struct TransformData
{
    public byte flags;
    public uint inputSequenceID;
    public float[] position;
    public float[] rotation;
    public float[] scale;
}

public class TransformPacket : UserNetworkPacket<TransformData>
{
    const ushort PositionBit = 1;
    const ushort RotationBit = 2;
    const ushort ScaleBit = 4;
    const ushort InputSequenceIDBit = 8;

    public TransformPacket() : base((ushort)UserPacketType.Transform)
    {

    }

    protected override void OnSerialize(Stream stream)
    {
        BinaryWriter binaryWriter = new BinaryWriter(stream);
        
        binaryWriter.Write(Payload.flags);
        
        if ((Payload.flags & PositionBit) != 0)
            for (int i = 0; i < Payload.position.Length; i++)
                binaryWriter.Write(Payload.position[i]);
        if ((Payload.flags & RotationBit) != 0)
            for (int i = 0; i < Payload.rotation.Length; i++)
                binaryWriter.Write(Payload.rotation[i]);
        if ((Payload.flags & ScaleBit) != 0)
            for (int i = 0; i < Payload.scale.Length; i++)
                binaryWriter.Write(Payload.scale[i]);
        if ((Payload.flags & InputSequenceIDBit) != 0)
            binaryWriter.Write(Payload.inputSequenceID);
    }
    
    protected override void OnDeserialize(Stream stream)
    {
        BinaryReader binaryReader = new BinaryReader(stream);

        TransformData transformData;
        
        transformData.flags = binaryReader.ReadByte();
        transformData.position = null;
        transformData.rotation = null;
        transformData.scale = null;
        transformData.inputSequenceID = 0;
        
        if ((transformData.flags & PositionBit) != 0)
        {
            transformData.position = new float[3];
            for (int i = 0; i < transformData.position.Length; i++)
                transformData.position[i] = binaryReader.ReadSingle();
        }
        if ((transformData.flags & RotationBit) != 0)
        {
            transformData.rotation = new float[4]; 
            for (int i = 0; i < transformData.rotation.Length; i++)
                transformData.rotation[i] = binaryReader.ReadSingle();
        }
        if ((transformData.flags & ScaleBit) != 0)
        {
            transformData.scale = new float[3];
            for (int i = 0; i < transformData.scale.Length; i++)
                transformData.scale[i] = binaryReader.ReadSingle();
        }
        if ((Payload.flags & InputSequenceIDBit) != 0)
            transformData.inputSequenceID = binaryReader.ReadUInt32();

        Payload = transformData;
    }
}