using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Master.Readers;

internal struct PrimitiveReader<T> : IColumnReader<T>
    where T : unmanaged
{
    private ReadOnlyMemory<byte> _data;
    public int Length { get; }
    public int Index { get; private set; } = 0;

    internal PrimitiveReader(ReadOnlyMemory<byte> data)
    {
        Length = data.Length / Unsafe.SizeOf<T>();
        _data = data;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<byte> Slice(int offset, int size)
    {
        size *= Unsafe.SizeOf<T>();
        int start = (Index + offset) * Unsafe.SizeOf<T>();
        if ((uint)start + size > (uint)_data.Length)
            throw new IndexOutOfRangeException();

        ReadOnlySpan<byte> slice = _data.Slice(start, size).Span;
        return slice;
    }
    
    public void Advance(int units)
    {
        Index += units;
    }


    public T Peek(int offset = 0)
    {
        return 
            typeof(T) == typeof(sbyte) ? Unsafe.BitCast<sbyte, T>((sbyte)Slice(offset, 1)[0]) :
            typeof(T) == typeof(short) ? Unsafe.BitCast<short, T>(BinaryPrimitives.ReadInt16LittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(int) ? Unsafe.BitCast<int, T>(BinaryPrimitives.ReadInt32LittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(long) ? Unsafe.BitCast<long, T>(BinaryPrimitives.ReadInt64LittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(byte) ? Unsafe.BitCast<byte, T>(Slice(offset, Unsafe.SizeOf<byte>())[0]) :
            typeof(T) == typeof(ushort) ? Unsafe.BitCast<ushort, T>(BinaryPrimitives.ReadUInt16LittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(uint) ? Unsafe.BitCast<uint, T>(BinaryPrimitives.ReadUInt32LittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(ulong) ? Unsafe.BitCast<ulong, T>(BinaryPrimitives.ReadUInt64LittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(Half) ? Unsafe.BitCast<Half, T>(BinaryPrimitives.ReadHalfLittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(float) ? Unsafe.BitCast<float, T>(BinaryPrimitives.ReadSingleLittleEndian(Slice(offset, 1))) :
            typeof(T) == typeof(double) ? Unsafe.BitCast<double, T>(BinaryPrimitives.ReadDoubleLittleEndian(Slice(offset, 1))) :
            throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);
    }

    public IEnumerable<T> Peek(int offset, int count)
    {
        T[] arr = new T[count];
        for (int i = 0; i < count; i++)
        {
            arr[i] = Peek(offset + i);
        }

        return arr;
    }
}