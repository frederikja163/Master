using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Master.Serializing.Columns;
using Master.Serializing.Readers;

namespace Master.Serializing;

public sealed class DataColumnReader<T> : IColumnReader<T>
{
    private readonly ReadOnlyMemory<byte> _data;
    private readonly LogicalType _logicalType;

    public DataColumnReader(DataColumn dataColumn)
    {
        _data = dataColumn.Data;
        _logicalType = dataColumn.LogicalType;
        Length = dataColumn.LogicalLength;
    }
    
    public int PhysicalSize => _data.Length;
    public bool AtEnd => _byteIndex >= _data.Length;

    public int Length { get; }
    private int _byteIndex = 0;
    public int Index { get; private set; } = 0;

    public void AdvanceRaw(int byteCount, int units)
    {
        if ((uint)_byteIndex + byteCount > _data.Length || (uint)units > Length)
            throw new IndexOutOfRangeException();
        _byteIndex += byteCount;
        Index += units;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<byte> Slice(int offset, int size)
    {
        int start = _byteIndex + offset;
        if ((uint)start + size > (uint)_data.Length)
            throw new IndexOutOfRangeException();

        ReadOnlySpan<byte> slice = _data.Slice(start, size).Span;
        return slice;
    }

    public T1 Peek<T1>(int offset = 0)
    {
        // TODO: This function only works properly where T : unmanaged, since the offset is in byte counts. This means we need a separate function for T Peek that can properly have an offset for var length types.
        return 
            typeof(T1) == typeof(sbyte) ? Unsafe.BitCast<sbyte, T1>((sbyte)Slice(offset, Unsafe.SizeOf<sbyte>())[0]) :
            typeof(T1) == typeof(short) ? Unsafe.BitCast<short, T1>(BinaryPrimitives.ReadInt16LittleEndian(Slice(offset, Unsafe.SizeOf<short>()))) :
            typeof(T1) == typeof(int) ? Unsafe.BitCast<int, T1>(BinaryPrimitives.ReadInt32LittleEndian(Slice(offset, Unsafe.SizeOf<int>()))) :
            typeof(T1) == typeof(long) ? Unsafe.BitCast<long, T1>(BinaryPrimitives.ReadInt64LittleEndian(Slice(offset, Unsafe.SizeOf<long>()))) :
            typeof(T1) == typeof(byte) ? Unsafe.BitCast<byte, T1>(Slice(offset, Unsafe.SizeOf<byte>())[0]) :
            typeof(T1) == typeof(ushort) ? Unsafe.BitCast<ushort, T1>(BinaryPrimitives.ReadUInt16LittleEndian(Slice(offset, Unsafe.SizeOf<ushort>()))) :
            typeof(T1) == typeof(uint) ? Unsafe.BitCast<uint, T1>(BinaryPrimitives.ReadUInt32LittleEndian(Slice(offset, Unsafe.SizeOf<uint>()))) :
            typeof(T1) == typeof(ulong) ? Unsafe.BitCast<ulong, T1>(BinaryPrimitives.ReadUInt64LittleEndian(Slice(offset, Unsafe.SizeOf<ulong>()))) :
            typeof(T1) == typeof(Half) ? Unsafe.BitCast<Half, T1>(BinaryPrimitives.ReadHalfLittleEndian(Slice(offset, Unsafe.SizeOf<Half>()))) :
            typeof(T1) == typeof(float) ? Unsafe.BitCast<float, T1>(BinaryPrimitives.ReadSingleLittleEndian(Slice(offset, Unsafe.SizeOf<float>()))) :
            typeof(T1) == typeof(double) ? Unsafe.BitCast<double, T1>(BinaryPrimitives.ReadDoubleLittleEndian(Slice(offset, Unsafe.SizeOf<double>()))) :
            typeof(T1) == typeof(byte[]) ? UnsafeAs<byte[], T1>(PeekBlob(offset).ToArray()) :
            typeof(T1) == typeof(string) ? UnsafeAs<string, T1>(Encoding.UTF8.GetString(PeekBlob(offset))) :
            throw new ArgumentOutOfRangeException(nameof(T), typeof(T), null);

        static TOut UnsafeAs<TIn, TOut>(TIn value)
        {
            return Unsafe.As<TIn, TOut>(ref value);
        }
    }

    private ReadOnlySpan<byte> PeekBlob(int offset = 0)
    {
        int length = Peek<int>();
        int totalOffset = 0 + Unsafe.SizeOf<int>();
        for (int i = 0; i < offset; i++)
        {
            totalOffset += length;
            length = Peek<int>(totalOffset);
            totalOffset += Unsafe.SizeOf<int>();
        }

        return Slice(totalOffset, length).ToArray();
    }

    public T1 Read<T1>()
    {
        T1 value = Peek<T1>();
        AdvanceRaw(Unsafe.SizeOf<T1>(), Unsafe.SizeOf<T1>() / Unsafe.SizeOf<T>());
        return value;
    }

    public IEnumerable<T1> Read<T1>(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return Read<T1>();
        }
    }

    public void Advance(int units = 1)
    {
        if (_logicalType.TryGetSize(out int size))
        {
            AdvanceRaw(units * size, 1);
            return;
        }

        for (int i = 0; i < units; i++)
        {
            int length = Peek<int>();
            AdvanceRaw(length + Unsafe.SizeOf<int>(), 1);
        }
    }

    public ReadOnlySpan<byte> ReadUnits(int count = 1)
    {
        if (_logicalType.TryGetSize(out int size))
        {
            return Slice(0, count * size);
        }

        int length = 0;
        for (int i = 0; i < count; i++)
        {
            length += Peek<int>(length) + Unsafe.SizeOf<int>();
        }

        return Slice(0, length);
    }

    public T Peek(int offset = 0)
    {
        return Peek<T>(offset * Unsafe.SizeOf<T>());
    }

    public IEnumerable<T> Peek(int count, int offset)
    {
        for (int i = 0; i < offset; i++)
        {
            yield return Peek(i);
        }
    }
}