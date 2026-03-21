using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TapResult.Readers;

/// <summary>
/// A generic reader for DataColumns. Can be opened with DataColumn.OpenGenericReader.
/// Reads values out from a ReadonlySpan&lt;T&gt; as arbitrarily typed values.
/// For example, it can read bit-casted ints out as longs etc.
/// The casting will be with the raw byte values in little endian.
/// </summary>
public sealed class GenericReader
{
    private readonly ReadOnlyMemory<byte> _data;
    /// <summary>
    /// The current index in bytes of the GenericReader.
    /// </summary>
    public int ByteIndex { get; private set; } = 0;

    /// <summary>
    /// Opens a new GenericReader.
    /// If one is being created from a DataColumn, it is recommended to use DataColumn.OpenGenericReader instead.
    /// </summary>
    public GenericReader(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }
    
    /// <summary>
    /// The physical size of this GenericReader.
    /// </summary>
    public int PhysicalSize => _data.Length;
    /// <summary>
    /// True if this GenericReader is at the end of reading.
    /// </summary>
    public bool IsAtEnd => ByteIndex == _data.Length;
    
    /// <summary>
    /// Advance the reader a number of bytes forward.
    /// Not needed to be called when reading as it advances for you.
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

        ReadOnlySpan<byte> slice = _data.Span.Slice(start, size);
        return slice;
    }

    /// <summary>
    /// Returns the next value from the data but does not consume it.
    /// T has to be a primitive type, for blobs and strings see their respective functions.
    /// Use <see cref="Read{T}()"/> if advancing is also needed.
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
    /// Reads the next value from the data and advances the position by the size of the value.
    /// </summary>
    public T Read<T>() where T : unmanaged
    {
        T value = Peek<T>();
        Advance(Unsafe.SizeOf<T>());
        return value;
    }

    /// <summary>
    /// Read a number of values out of the GenericReader and advances to the value after the last one.
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
    /// Read a single length prefixed blob and advances the reader.
    /// </summary>
    public ReadOnlySpan<byte> ReadBlob()
    {
        int length = Read<int>();
        return Read<byte>(length);
    }

    /// <summary>
    /// Reads multiple length prefixed blobs and advances the reader.
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
    /// Read a single length prefixed UTF8 string and advances the reader.
    /// </summary>
    public string ReadString()
    {
        ReadOnlySpan<byte> blob = ReadBlob();
        return Encoding.UTF8.GetString(blob);
    }

    /// <summary>
    /// Reads multiple length prefixed UTF8 strings and advances the reader.
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
    /// Advances an amount of bytes forward equivalent to 'count' number of 'type' elements.
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
    /// Reads an amount of bytes equivalent to 'count' number of 'type' elements.
    /// However, the values are kept as a ReadonlySpan&lt;byte&gt; rather than being cast into their types like <see cref="Read{T}(int)"/>
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