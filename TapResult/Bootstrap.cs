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
    private static ulong MagicNumberStart =
        ((ulong)'O' << (7 * 8)) |
        ((ulong)'T' << (6 * 8)) |
        ((ulong)'A' << (5 * 8)) |
        ((ulong)'P' << (4 * 8));

    public static ulong SerializeMagicNumber(FileType type, byte major, byte minor, byte patch)
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

    public static byte[] SerializeTapResultPostfix(long metadataStart, long length, long logicalLength, ulong magicNumber)
    {
        byte[] data = new byte[Unsafe.SizeOf<ulong>() * 4];
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(0 * Unsafe.SizeOf<ulong>()), metadataStart);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(1 * Unsafe.SizeOf<ulong>()), length);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(2 * Unsafe.SizeOf<ulong>()), logicalLength);
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(3 * Unsafe.SizeOf<ulong>()), magicNumber);
        return data;
    }
    
    public static long ParseTapResultPostfix(ReadOnlyMemory<byte> postfix, out long length, out long logicalLength, out ulong magicNumber)
    {
        GenericReader postfixReader = new (postfix);
        long start = postfixReader.Read<long>();
        length = postfixReader.Read<long>();
        logicalLength = (int)postfixReader.Read<long>();
        magicNumber = postfixReader.Read<ulong>();
        return start;
    }
}