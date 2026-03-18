using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace TapResult.Readers;

public class RunLengthReader<T>(IColumnReader<byte> byteColumn, IColumnReader<int> repeatColumn, int byteLength, int length) : IColumnReader<T>
    where T : unmanaged, INumber<T>, IBinaryInteger<T>, IMinMaxValue<T>
{
    public int ByteLength { get; } = byteLength;
    public int Length { get; } = length;
    public int Index => byteColumn.Index;

    public void Advance(int units)
    {
        byteColumn.Advance(units);
        repeatColumn.Advance(units);
    }

    public T Peek(int offset = 0)
    {
        offset = Index + offset;
        int index = 0;
        int currentOffset = 0;
        int currentVal;
        while (currentOffset < offset)
        {
            currentVal = repeatColumn.Peek(index);
            index++;
            currentOffset += currentVal;
        }
        ReadOnlySpan<byte> slice = byteColumn.Peek(index, Length).ToArray();
        return 
            typeof(T) == typeof(sbyte) ? Unsafe.BitCast<sbyte, T>((sbyte)slice[0]) :
            typeof(T) == typeof(short) ? Unsafe.BitCast<short, T>(BinaryPrimitives.ReadInt16LittleEndian(slice)) :
            typeof(T) == typeof(int) ? Unsafe.BitCast<int, T>(BinaryPrimitives.ReadInt32LittleEndian(slice)) :
            typeof(T) == typeof(long) ? Unsafe.BitCast<long, T>(BinaryPrimitives.ReadInt64LittleEndian(slice)) :
            typeof(T) == typeof(byte) ? Unsafe.BitCast<byte, T>(slice[0]) :
            typeof(T) == typeof(ushort) ? Unsafe.BitCast<ushort, T>(BinaryPrimitives.ReadUInt16LittleEndian(slice)) :
            typeof(T) == typeof(uint) ? Unsafe.BitCast<uint, T>(BinaryPrimitives.ReadUInt32LittleEndian(slice)) :
            typeof(T) == typeof(ulong) ? Unsafe.BitCast<ulong, T>(BinaryPrimitives.ReadUInt64LittleEndian(slice)) :
            typeof(T) == typeof(Half) ? Unsafe.BitCast<Half, T>(BinaryPrimitives.ReadHalfLittleEndian(slice)) :
            typeof(T) == typeof(float) ? Unsafe.BitCast<float, T>(BinaryPrimitives.ReadSingleLittleEndian(slice)) :
            typeof(T) == typeof(double) ? Unsafe.BitCast<double, T>(BinaryPrimitives.ReadDoubleLittleEndian(slice)) :
            throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);
    }

    public IEnumerable<T> Peek(int offset, int count)
    {
        offset = Index + offset;
        int index = 0;
        int currentOffset = 0;
        int currentVal;
        while (currentOffset < offset)
        {
            currentVal = repeatColumn.Peek(index);
            index++;
            currentOffset += currentVal;
        }

        int currentRepeat = currentOffset - offset;
        while (count > 0)
        {
            for (int i = 0; i < currentRepeat; i++)
            {
                if (count-- > 0)
                {
                    yield return (T) byteColumn.Peek(index, Length);
                }
            }
            index++;
            currentRepeat = repeatColumn.Peek(index);
        }
    }
}