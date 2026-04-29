using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using TapResult.Readers;

namespace TapResult;

public enum FileType : byte
{
    TapResult = (byte)'R',
    TapSchema = (byte)'S',
    TapData = (byte)'D',
}

public static class Bootstrap
{
    private const ulong MagicNumberStart =
        ((ulong)'O' << (7 * 8)) |
        ((ulong)'T' << (6 * 8)) |
        ((ulong)'A' << (5 * 8)) |
        ((ulong)'P' << (4 * 8));

    public static readonly int BootstrapSize = Unsafe.SizeOf<long>() * 4;
    
    public static ulong GetMagicNumber(FileType type, byte major, byte minor, byte patch)
    {
        return MagicNumberStart | 
               ((ulong)type << (3 * 8)) |
               ((ulong)major << (2 * 8)) |
               ((ulong)minor << (1 * 8)) |
               ((ulong)patch << (0 * 8));
    }
    

    public static bool TryParseMagicNumber(ulong magicNumber, out FileType type, out byte major, out byte minor, out byte patch)
    {
        patch = (byte)((magicNumber >> (0 * 8)) & byte.MaxValue);
        minor = (byte)((magicNumber >> (1 * 8)) & byte.MaxValue);
        major = (byte)((magicNumber >> (2 * 8)) & byte.MaxValue);
        type = (FileType)((magicNumber >> (3 * 8)) & byte.MaxValue);
        ulong start = magicNumber >> (4 * 8);
        return MagicNumberStart == start;
    }

    public static void SerializePostfix(Span<byte> bootstrap, long start, long length, long logicalLength, ulong magicNumber)
    {
        BinaryPrimitives.WriteInt64LittleEndian(bootstrap.Slice(0 * Unsafe.SizeOf<ulong>()), start);
        BinaryPrimitives.WriteInt64LittleEndian(bootstrap.Slice(1 * Unsafe.SizeOf<ulong>()), length);
        BinaryPrimitives.WriteInt64LittleEndian(bootstrap.Slice(2 * Unsafe.SizeOf<ulong>()), logicalLength);
        BinaryPrimitives.WriteUInt64LittleEndian(bootstrap.Slice(3 * Unsafe.SizeOf<ulong>()), magicNumber);
    }
    
    public static void ParsePostfix(ReadOnlySpan<byte> bootstrap, out long start, out long length, out long logicalLength, out ulong magicNumber)
    {
        start = BinaryPrimitives.ReadInt64LittleEndian(bootstrap.Slice(0 * Unsafe.SizeOf<ulong>()));
        length = BinaryPrimitives.ReadInt64LittleEndian(bootstrap.Slice(1 * Unsafe.SizeOf<ulong>()));
        logicalLength = BinaryPrimitives.ReadInt64LittleEndian(bootstrap.Slice(2 * Unsafe.SizeOf<ulong>()));
        magicNumber = BinaryPrimitives.ReadUInt64LittleEndian(bootstrap.Slice(3 * Unsafe.SizeOf<ulong>()));
    }

    public static void SerializePrefix(Span<byte> bootstrap, long start, long length, long logicalLength)
    {
        BinaryPrimitives.WriteInt64LittleEndian(bootstrap.Slice(0 * Unsafe.SizeOf<ulong>()), start);
        BinaryPrimitives.WriteInt64LittleEndian(bootstrap.Slice(1 * Unsafe.SizeOf<ulong>()), length);
        BinaryPrimitives.WriteInt64LittleEndian(bootstrap.Slice(2 * Unsafe.SizeOf<ulong>()), logicalLength);
    }
    
    public static void ParsePrefix(ReadOnlySpan<byte> bootstrap, out long start, out long length, out long logicalLength)
    {
        start = BinaryPrimitives.ReadInt64LittleEndian(bootstrap.Slice(0 * Unsafe.SizeOf<ulong>()));
        length = BinaryPrimitives.ReadInt64LittleEndian(bootstrap.Slice(1 * Unsafe.SizeOf<ulong>()));
        logicalLength = BinaryPrimitives.ReadInt64LittleEndian(bootstrap.Slice(2 * Unsafe.SizeOf<ulong>()));
    }
}