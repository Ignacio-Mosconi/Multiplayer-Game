using System;

public class CrcCalculator
{
    uint[] crcTable;
    uint polynomialDivisor;
    
    const int BitsInByte = 8;
    const int ByteValues = 1 << BitsInByte;
    const uint Msb32 = unchecked((uint)(1 << (sizeof(int) * BitsInByte - 1)));
    const int Msb32Displacement = (sizeof(int) * BitsInByte) - BitsInByte;

    public CrcCalculator(uint polynomialDivisor)
    {
        this.polynomialDivisor = polynomialDivisor;
        crcTable = new uint[ByteValues];
        UpdateCrcTable();
    }

    void UpdateCrcTable()
    {
        for (int divident = 0; divident < ByteValues; divident++)
        {
            uint currentByte = (uint)(divident << Msb32Displacement);

            for (int i = 0; i < BitsInByte; i++)
            {
                if ((currentByte & Msb32) != 0)
                {
                    currentByte <<= 1;
                    currentByte ^= polynomialDivisor;
                }
                else
                    currentByte <<= 1;
            }

            crcTable[divident] = currentByte;
        }
    }

    public uint ComputeCrc32(byte[] data)
    {
        uint crc = 0;

        for (int i = 0; i < data.Length; i++)
        {
            int position = (int)(crc >> Msb32Displacement) ^ data[i];

            crc = (uint)(crc << BitsInByte) ^ crcTable[position];
        }

        return crc;
    }

    public bool PerformCrcCheck(byte[] data, uint crc)
    {
        return (ComputeCrc32(data) == crc);
    }
}