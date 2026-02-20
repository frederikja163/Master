using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using Master.Extensions;
using Master.Serializing;

namespace Master.Tests;

internal sealed class DataColumnTests
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
    
    [Test]
    public void CreateStringsTest()
    {
        string[] strings = ["Abcd", "1234", "Hello world!", "CSharp"];
        DataColumn column = DataColumn.Create(strings);
        int physicalLength = strings.Select(str => Unsafe.SizeOf<int>() + Encoding.UTF8.GetByteCount(str)).Sum();
        Assert.That(column.PhysicalSize, Is.EqualTo(physicalLength));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.String));
        Assert.That(column.LogicalLength, Is.EqualTo(strings.Length));
        
        byte[] data = strings.SelectMany(GetBytes).ToArray();
        Assert.That(column.Data.ToArray(), Is.EquivalentTo(data));
    }
    
    [Test]
    public void CreateBlobsTest()
    {
        byte[][] blobs = [[0, 1, 2], [1,2,3], [2,3,4]];
        DataColumn column = DataColumn.Create(blobs);
        int physicalLength = blobs.Select(blob => Unsafe.SizeOf<int>() + blob.Length).Sum();
        Assert.That(column.PhysicalSize, Is.EqualTo(physicalLength));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.Blob));
        Assert.That(column.LogicalLength, Is.EqualTo(blobs.Length));

        byte[] data = blobs.SelectMany(GetBytes).ToArray();
        Assert.That(column.Data.ToArray(), Is.EquivalentTo(data));
    }

    public static IEnumerable<(Array, byte[], LogicalType type)> CreatePrimitivesTestSource()
    {
        foreach ((Array array, LogicalType type) in CreateArrays())
        {
            yield return (array, array.Cast<object>().SelectMany(GetBytes).ToArray(), type);
        }

        static IEnumerable<(Array, LogicalType)> CreateArrays()
        {
            yield return (Enumerable.Range(0, 8).Select(i => (sbyte)i).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (short)i).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (int)i).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (long)i).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (byte)i).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (ushort)i).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (uint)i).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (ulong)i).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (Half)i).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 8).Select(i => (float)i).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 8).Select(i => (double)i).ToArray(), LogicalType.Float64);
            
            yield return (Enumerable.Range(0, 8).Select(i => (sbyte?)i).ToArray(), LogicalType.SInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (short?)i).ToArray(), LogicalType.SInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (int?)i).ToArray(), LogicalType.SInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (long?)i).ToArray(), LogicalType.SInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (byte?)i).ToArray(), LogicalType.UInt8);
            yield return (Enumerable.Range(0, 8).Select(i => (ushort?)i).ToArray(), LogicalType.UInt16);
            yield return (Enumerable.Range(0, 8).Select(i => (uint?)i).ToArray(), LogicalType.UInt32);
            yield return (Enumerable.Range(0, 8).Select(i => (ulong?)i).ToArray(), LogicalType.UInt64);
            yield return (Enumerable.Range(0, 8).Select(i => (Half?)i).ToArray(), LogicalType.Float16);
            yield return (Enumerable.Range(0, 8).Select(i => (float?)i).ToArray(), LogicalType.Float32);
            yield return (Enumerable.Range(0, 8).Select(i => (double?)i).ToArray(), LogicalType.Float64);
        }
    }
    
    [Test]
    [TestCaseSource(nameof(CreatePrimitivesTestSource))]
    public void CreatePrimitivesTest((Array array, byte[] bytes, LogicalType type) tuple)
    {
        (Array array, byte[] bytes, LogicalType type) = tuple;
        DataColumn column = DataColumn.Create(array, out DataColumn? nulls);
        Assert.That(column.LogicalType, Is.EqualTo(type));
        Assert.That(column.Data.ToArray(), Is.EquivalentTo(bytes));
        Assert.That(column.LogicalLength, Is.EqualTo(array.Length));

        if (!array.GetType().GetElementType()!.IsNullable())
        {
            Assert.That(nulls.HasValue, Is.EqualTo(false));
        }
        else
        {
            
            Assert.That(nulls.HasValue, Is.EqualTo(true));
            DataColumn nullsCol = nulls.Value;
            Assert.That(nullsCol.LogicalType, Is.EqualTo(LogicalType.UInt8));
            Assert.That(nullsCol.LogicalLength, Is.EqualTo(array.Length / 8 + 1));
            Assert.That(nullsCol.PhysicalSize, Is.EqualTo(array.Length / 8 + 1));
            Assert.That(nullsCol.Data.ToArray(), Is.All.EqualTo(0));
        }
    }

    [Test]
    public void DataColumnCreateFailsOnInvalidType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DataColumn.Create(Array.Empty<object>(), out _));
    }
}