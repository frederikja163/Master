using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using TapResult.Tests.Extensions;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Tests;

internal sealed class ColumnBuilderTests
{
    [Test]
    public void WritePrimitiveTest()
    {
        ColumnBuilder<byte> builder = new ColumnBuilder<byte>(44);
        BlobBuilder blob = builder.OpenBlob();
        blob.WriteValue<sbyte>(1);
        blob.WriteValue<short>(2);
        blob.WriteValue<int>(3);
        blob.WriteValue<long>(4);
        blob.WriteValue<byte>(5);
        blob.WriteValue<ushort>(6);
        blob.WriteValue<uint>(7);
        blob.WriteValue<ulong>(8);
        blob.WriteValue<Half>((Half)9);
        blob.WriteValue<float>(10);
        blob.WriteValue<double>(11);
        blob.Dispose();
        DataColumn column = Assert.InstanceOf<DataColumn>(builder.BuildDataColumn());
        Assert.That(column.LogicalLength, Is.EqualTo(1));
        Assert.That(column.PhysicalSize, Is.EqualTo(44));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.UInt8));

        byte[] bytes = new byte[14];
        BinaryPrimitives.WriteHalfLittleEndian(bytes, (Half)9);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(2), 10);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(6), 11);
        Assert.That(column.Data.ToArray(), Is.EqualTo(new byte[]{
            1,
            2, 0,
            3, 0, 0, 0,
            4, 0, 0, 0, 0, 0, 0, 0,
            5,
            6, 0,
            7, 0, 0, 0,
            8, 0, 0, 0, 0, 0, 0, 0,
        }.Concat(bytes)));
    }

    [Test]
    public void WriteStrings()
    {
        string[] strs = ["test", "hello world", "abcd1234"];
        int length = strs.Select(Encoding.UTF8.GetByteCount).Sum() + strs.Length * Unsafe.SizeOf<int>();
        
        ColumnBuilder<string> builder = new(length);
        builder.WriteValues(strs);
        SplitColumn column = Assert.InstanceOf<SplitColumn>(builder.Build());
        Assert.That(column.LogicalLength, Is.EqualTo(strs.Length));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.String));
        Assert.That(column.OpenReader<string>().Read(strs.Length), Is.EqualTo(strs));
    }

    [Test]
    public void WriteBlobs()
    {
        string[] strs = ["test", "hello world", "abcd1234"];
        int length = strs.Select(Encoding.UTF8.GetByteCount).Sum() + strs.Length * Unsafe.SizeOf<int>();
        
        ColumnBuilder<byte[]> builder = new ColumnBuilder<byte[]>(length);
        builder.WriteValues(strs.Select(Encoding.UTF8.GetBytes).ToArray());
        SplitColumn column = Assert.InstanceOf<SplitColumn>(builder.Build(LogicalType.Blob));
        Assert.That(column.LogicalLength, Is.EqualTo(strs.Length));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.Blob));
        Assert.That(column.OpenReader<byte[]>().Read(strs.Length), Is.EqualTo(strs));
    }

    [Test]
    public void CanResizeTest()
    {
        ColumnBuilder<byte> builder = new (1);
        builder.WriteValue(123);
        Assert.That(builder.PhysicalSize, Is.EqualTo(1));
        builder.WriteValue(123);
        Assert.That(builder.PhysicalSize, Is.EqualTo(2));
        builder.WriteValue(21);
        Assert.That(builder.PhysicalSize, Is.EqualTo(3));
        DataColumn column = Assert.InstanceOf<DataColumn>(builder.BuildDataColumn());
        Assert.That(column.LogicalLength, Is.EqualTo(3));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(column.PhysicalSize, Is.EqualTo(3));
        Assert.That(column.Data.ToArray(), Is.EqualTo(new byte[]{123, 123, 21}));
    }
}