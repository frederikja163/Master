using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace Master.Tests;

public static class DataHelper
{
    public static IEnumerable<byte> GetBytes(object value)
    {
        byte[] bytes = new byte[10];
        int size = 0;
        switch (value)
        {
            case sbyte sInt8: size = Unsafe.SizeOf<sbyte>(); bytes[0] = (byte)sInt8; break;
            case short sInt16: size = Unsafe.SizeOf<short>(); BinaryPrimitives.WriteInt16LittleEndian(bytes, sInt16); break;
            case int sInt32: size = Unsafe.SizeOf<int>(); BinaryPrimitives.WriteInt32LittleEndian(bytes, sInt32); break;
            case long sInt64: size = Unsafe.SizeOf<long>(); BinaryPrimitives.WriteInt64LittleEndian(bytes, sInt64); break;
            case byte uInt8: size = Unsafe.SizeOf<byte>(); bytes[0] = uInt8; break;
            case ushort uInt16: size = Unsafe.SizeOf<ushort>(); BinaryPrimitives.WriteUInt16LittleEndian(bytes, uInt16); break;
            case uint uInt32: size = Unsafe.SizeOf<uint>(); BinaryPrimitives.WriteUInt32LittleEndian(bytes, uInt32); break;
            case ulong uInt64: size = Unsafe.SizeOf<ulong>(); BinaryPrimitives.WriteUInt64LittleEndian(bytes, uInt64); break;
            case Half float16: size = Unsafe.SizeOf<Half>(); BinaryPrimitives.WriteHalfLittleEndian(bytes, float16); break;
            case float float32: size = Unsafe.SizeOf<float>(); BinaryPrimitives.WriteSingleLittleEndian(bytes, float32); break;
            case double float64: size = Unsafe.SizeOf<double>(); BinaryPrimitives.WriteDoubleLittleEndian(bytes, float64); break;
            case string str:
                return GetBytes(str.Length).Concat(Encoding.UTF8.GetBytes(str));
            case byte[] blob:
                return GetBytes(blob.Length).Concat(blob);
        }

        return bytes.Take(size);
    }
}