using TapResult;
using TapResult.Columns;
using TapResult.Readers;
using TapResult.Tests.Extensions;

namespace TapResult.Tests;

internal sealed class GenericReaderTests
{
    [Test]
    public void AdvanceTest()
    {
        DataColumn column = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(Enumerable.Range(0, 100).ToArray()));
        IColumnReader<int> reader = column.OpenReader<int>();
        reader.Advance(12);
        Assert.That(reader.Read(), Is.EqualTo(12));
        reader.Advance(40);
        Assert.That(reader.Read(), Is.EqualTo(53));
    }

    [Test]
    public void OverFlowTest()
    {
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenGenericReader().Advance(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenGenericReader().Advance(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenGenericReader().Peek<int>());
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenGenericReader().Read<int>());
    }
    
    [Test]
    public void ReadPrimitiveTest()
    {
        ColumnBuilder<byte> builder = new (402);
        builder.WriteValue(123);
        builder.WriteValue(123);
        builder.WriteValues(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());
        GenericReader reader = Assert.InstanceOf<DataColumn>(builder.BuildDataColumn()).OpenGenericReader();

        Assert.That(reader.Peek<byte>(), Is.EqualTo((byte)123));
        Assert.That(reader.Peek<byte>(1), Is.EqualTo((int)123));
        Assert.That(reader.IsAtEnd, Is.False);
        
        Assert.That(reader.Read<byte>(), Is.EqualTo((byte)123));
        Assert.That(reader.Peek<byte>(), Is.EqualTo(123));
        Assert.That(reader.IsAtEnd, Is.False);

        Assert.That(reader.Read<byte>(), Is.EqualTo(123));
        Assert.That(reader.Read<byte>(100).ToArray(), Is.EqualTo(Enumerable.Range(0, 100)));
        Assert.That(reader.IsAtEnd, Is.True);
    }

    [Test]
    public void ReadVariableLengthUnitsTest()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        ColumnBuilder<byte[]> builder = new ColumnBuilder<byte[]>(strings.Length);
        using (BlobBuilder blobBuilder = builder.OpenBlob())
        {
            foreach (string blob in strings)
            {
                blobBuilder.WriteValue(blob);
            }
        }
        DataColumn column = Assert.InstanceOf<DataColumn>(builder.BuildDataColumn());
        GenericReader reader = column.OpenGenericReader();
        Assert.That(reader.ReadUnits(LogicalType.String, 3).ToArray(),
            Is.EqualTo(column.Data.Span.Slice(0, 36).ToArray()));
    }

    [Test]
    public void ReadMultiplePrimitivesTest()
    {
        float[] data = Enumerable.Range(0, 100).Select(i => MathF.Sin(i / 10f)).ToArray();
        DataColumn column = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data));
        IColumnReader<float> reader = column.OpenReader<float>();
        
        Assert.That(reader.Read(40).ToArray(), Is.EqualTo(data.Take(40)));
    }

    [Test]
    public void ReadStringsTests()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        ColumnBuilder<byte[]> builder = new ColumnBuilder<byte[]>(strings.Length);
        using (BlobBuilder blobBuilder = builder.OpenBlob())
        {
            foreach (string blob in strings)
            {
                blobBuilder.WriteValue(blob);
            }
        }
        DataColumn column = Assert.InstanceOf<DataColumn>(builder.BuildDataColumn());
        GenericReader reader = column.OpenGenericReader();

        Assert.That(reader.ReadString(), Is.EqualTo("test"));
        reader.AdvanceUnits(LogicalType.String, 2);
        Assert.That(reader.ReadString(4), Is.EqualTo(new string[] { "This", "Is", "Test", "Data" }));
        Assert.That(reader.IsAtEnd);
    }

    [Test]
    public void ReadBlobsTest()
    {
        byte[][] data = new byte[][]{"test"u8.ToArray(),
                "hello world"u8.ToArray(),
                "i am here"u8.ToArray(),
                "This"u8.ToArray(),
                "Is"u8.ToArray(),
                "Test"u8.ToArray(),
                "Data"u8.ToArray()};
        ColumnBuilder<byte[]> builder = new ColumnBuilder<byte[]>(data.Length);
        using (BlobBuilder blobBuilder = builder.OpenBlob())
        {
            foreach (byte[] blob in data)
            {
                blobBuilder.WriteValue(blob);
            }
        }
        DataColumn column = Assert.InstanceOf<DataColumn>(builder.BuildDataColumn());
        GenericReader reader = column.OpenGenericReader();

        Assert.That(reader.ReadBlob().ToArray(), Is.EqualTo("test"u8.ToArray()));
        reader.AdvanceUnits(LogicalType.Blob, 2);
        Assert.That(reader.ReadBlob(4), Is.EqualTo(new byte[][] { "This"u8.ToArray(), "Is"u8.ToArray(), "Test"u8.ToArray(), "Data"u8.ToArray() }));
        Assert.That(reader.IsAtEnd);
    }
}