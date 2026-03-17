using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TapResult.Readers;

/// <summary>
/// TODO
/// </summary>
public ref struct GenericReader
{
    private readonly ReadOnlySpan<byte> _data;
    /// <summary>
    /// TODO
    /// </summary>
    public int ByteIndex { get; private set; } = 0;

    /// <summary>
    /// TODO
    /// </summary>
    public GenericReader(ReadOnlySpan<byte> data)
    {
        _data = data;
    }
    
    /// <summary>
    /// TODO
    /// </summary>
    public int PhysicalSize => _data.Length;
    /// <summary>
    /// TODO
    /// </summary>
    public bool AtEnd => ByteIndex == _data.Length;
    
    /// <summary>
    /// TODO
    /// </summary>
    public void Advance(int byteCount)
    {
        int newIndex = ByteIndex + byteCount;
        if ((uint)newIndex > _data.Length)
            throw new IndexOutOfRangeException();
        ByteIndex = newIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private ReadOnlySpan<byte> Slice(int offset, int size)
    {
        int start = ByteIndex + offset;
        if ((uint)start + size > (uint)_data.Length)
            throw new IndexOutOfRangeException();

        ReadOnlySpan<byte> slice = _data.Slice(start, size);
        return slice;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public T Peek<T>(int byteOffset = 0)
        where T : unmanaged
    {
        ReadOnlySpan<byte> slice = Slice(byteOffset, Unsafe.SizeOf<T>());
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

    /// <summary>
    /// TODO
    /// </summary>
    public T Read<T>() where T : unmanaged
    {
        T value = Peek<T>();
        Advance(Unsafe.SizeOf<T>());
        return value;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public ReadOnlySpan<T> Read<T>(int count)
        where T : unmanaged
    {
        if (BitConverter.IsLittleEndian)
        {
            ReadOnlySpan<byte> slice = Slice(0, count * Unsafe.SizeOf<T>());
            Advance(count * Unsafe.SizeOf<T>());
            return MemoryMarshal.Cast<byte, T>(slice);
        }

        T[] values = new T[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = Read<T>();
        }

        return values;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public ReadOnlySpan<byte> ReadBlob()
    {
        int length = Read<int>();
        return Read<byte>(length);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public byte[][] ReadBlob(int length)
    {
        byte[][] values = new byte[length][];
        for (int i = 0; i < length; i++)
        {
            values[i] = ReadBlob().ToArray();
        }

        return values;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public string ReadString()
    {
        ReadOnlySpan<byte> blob = ReadBlob();
        return Encoding.UTF8.GetString(blob);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public string[] ReadString(int length)
    {
        string[] values = new string[length];
        for (int i = 0; i < length; i++)
        {
            values[i] = ReadString();
        }

        return values;
    }

    /// <summary>
    /// TODO
    /// </summary>
    public void AdvanceUnits(LogicalType type, int count = 1)
    {
        if (type.TryGetSize(out int size))
        {
            Advance(count * size);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            int length = Read<int>();
            Advance(length);
        }
    }

    /// <summary>
    /// TODO
    /// </summary>
    public ReadOnlySpan<byte> ReadUnits(LogicalType type, int count = 1)
    {
        if (type.TryGetSize(out int size))
        {
            return Read<byte>(count * size);
        }

        int length = 0;
        for (int i = 0; i < count; i++)
        {
            length += Peek<int>(length) + Unsafe.SizeOf<int>();
        }

        return Read<byte>(length);
    }
}