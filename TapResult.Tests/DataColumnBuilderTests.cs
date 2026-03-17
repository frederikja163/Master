using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using Master;
using Master.Columns;

namespace TapResult.Tests;

internal sealed class DataColumnBuilderTests
{
    [Test]
    public void WritePrimitiveTest()
    {
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.UInt8, 44);
        builder.Write<sbyte>(1);
        builder.Write<short>(2);
        builder.Write<int>(3);
        builder.Write<long>(4);
        builder.Write<byte>(5);
        builder.Write<ushort>(6);
        builder.Write<uint>(7);
        builder.Write<ulong>(8);
        builder.Write<Half>((Half)9);
        builder.Write<float>(10);
        builder.Write<double>(11);
        Assert.That(builder.IsAtEnd);
        DataColumn column = builder.Build();
        Assert.That(column.LogicalLength, Is.EqualTo(44));
        Assert.That(column.PhysicalSize, Is.EqualTo(44));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.UInt8));

        byte[] bytes = new byte[14];
        BinaryPrimitives.WriteHalfLittleEndian(bytes, (Half)9);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(2), 10);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.AsSpan(6), 11);
        new byte[]
        {
            1,
            2, 0,
            3, 0, 0, 0,
            4, 0, 0, 0, 0, 0, 0, 0,
            5,
            6, 0,
            7, 0, 0, 0,
            8, 0, 0, 0, 0, 0, 0, 0,
        }.Concat(bytes);
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
        
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.String, length);
        builder.WriteStrings(strs);
        Assert.That(builder.IsAtEnd, Is.True);
        DataColumn column = builder.Build();
        Assert.That(column.LogicalLength, Is.EqualTo(strs.Length));
        Assert.That(column.PhysicalSize, Is.EqualTo(length));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.String));
        Assert.That(column.Data.ToArray(), Is.EqualTo(strs.SelectMany(DataHelper.GetBytes)));
    }

    [Test]
    public void WriteBlobs()
    {
        string[] strs = ["test", "hello world", "abcd1234"];
        int length = strs.Select(Encoding.UTF8.GetByteCount).Sum() + strs.Length * Unsafe.SizeOf<int>();
        
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.Blob, length);
        builder.WriteBlobs(strs.Select(Encoding.UTF8.GetBytes));
        Assert.That(builder.IsAtEnd, Is.True);
        DataColumn column = builder.Build();
        Assert.That(column.LogicalLength, Is.EqualTo(strs.Length));
        Assert.That(column.PhysicalSize, Is.EqualTo(length));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.Blob));
        Assert.That(column.Data.ToArray(), Is.EqualTo(strs.SelectMany(DataHelper.GetBytes)));
    }

    [Test]
    public void CanResizeTest()
    {
        DataColumnBuilder builder = new DataColumnBuilder(1, false);
        builder.Write<byte>(123);
        Assert.That(builder.IsAtEnd, Is.True);
        Assert.That(builder.PhysicalSize, Is.EqualTo(1));
        builder.Write<byte>(123);
        Assert.That(builder.IsAtEnd, Is.True);
        Assert.That(builder.PhysicalSize, Is.EqualTo(2));
        builder.Write<byte>(21);
        Assert.That(builder.IsAtEnd, Is.False);
        Assert.That(builder.PhysicalSize, Is.EqualTo(3));
        DataColumn column = builder.Build();
        Assert.That(column.LogicalLength, Is.EqualTo(3));
        Assert.That(column.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(column.PhysicalSize, Is.EqualTo(3));
        Assert.That(column.Data.ToArray(), Is.EqualTo(new byte[]{123, 123, 21}));
    }

    [Test]
    public void IndexOutOfBoundsExceptionTest()
    {
        Assert.Throws<IndexOutOfRangeException>(() => { 
            DataColumnBuilder builder = new DataColumnBuilder(LogicalType.UInt8, 1);
            builder.Write(new byte[] { 123, 23 });
        });
    }
}