using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace TapResult.Extensions;

internal static class StreamExtensions
{
    public static void WriteUInt64(this Stream stream, ulong value)
    {
        Span<byte> bytes = stackalloc byte[Unsafe.SizeOf<ulong>()];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        stream.Write(bytes);
    }
}