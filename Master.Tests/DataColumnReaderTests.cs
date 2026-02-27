using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Readers;

namespace Master.Tests;

internal sealed class DataColumnReaderTests
{
    [Test]
    public void AdvanceTest()
    {
        DataColumn column = DataColumn.Create(Enumerable.Range(0, 100).ToArray());
        IColumnReader<int> reader = column.OpenReader<int>();
        reader.Advance(12);
        Assert.That(reader.Read(), Is.EqualTo(12));
        reader.Advance(40);
        Assert.That(reader.Read(), Is.EqualTo(53));
    }

    [Test]
    public void OverFlowTest()
    {
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader<int>().Advance(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader<int>().Advance(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader<int>().Peek());
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader<int>().Read());
    }
    
    [Test]
    public void ReadPrimitiveTest()
    {
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.SInt32, 405, false);
        builder.Write<byte>(123);
        builder.Write(123);
        builder.Write(Enumerable.Range(0, 100).ToArray());
        DataColumnReader<int> reader = builder.Build().OpenDataColumnReader<int>();
        Assert.That(reader.Peek<byte>(), Is.EqualTo((byte)123));
        Assert.That(reader.Peek<int>(1), Is.EqualTo((int)123));
        Assert.That(reader.AtEnd, Is.False);
        
        Assert.That(reader.Read<byte>(), Is.EqualTo((byte)123));
        Assert.That(reader.Peek<int>(), Is.EqualTo(123));
        Assert.That(reader.AtEnd, Is.False);

        Assert.That(reader.Read<int>(), Is.EqualTo(123));
        Assert.That(reader.Read<int>(100).ToArray(), Is.EquivalentTo(Enumerable.Range(0, 100)));
        Assert.That(reader.AtEnd, Is.True);
    }

    [Test]
    public void ReadVariableLengthUnitsTest()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        DataColumn column = DataColumn.Create(strings);
        DataColumnReader<string> reader = column.OpenDataColumnReader<string>();
        Assert.That(reader.ReadUnits(3).ToArray(),
            Is.EquivalentTo(column.Data.Span.Slice(0, 36).ToArray()));
    }

    [Test]
    public void ReadMultiplePrimitivesTest()
    {
        float[] data = Enumerable.Range(0, 100).Select(i => MathF.Sin(i / 10f)).ToArray();
        DataColumn column = DataColumn.Create(data);
        IColumnReader<float> reader = column.OpenReader<float>();
        
        Assert.That(reader.Read(40).ToArray(), Is.EquivalentTo(data.Take(40)));
    }

    [Test]
    public void ReadStringsTests()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        DataColumn column = DataColumn.Create(strings);
        IColumnReader<string> reader = column.OpenReader<string>();

        Assert.That(reader.Read(), Is.EqualTo("test"));
        reader.Advance(2);
        Assert.That(reader.Read(4), Is.EquivalentTo(new string[] { "This", "Is", "Test", "Data" }));
        Assert.That(reader.IsAtEnd);
    }

    [Test]
    public void ReadBlobsTest()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        DataColumn column = DataColumn.Create(strings);
        IColumnReader<byte[]> reader = column.OpenReader<byte[]>();

        Assert.That(reader.Read(), Is.EquivalentTo("test"u8.ToArray()));
        Assert.That(reader.Peek(2), Is.EquivalentTo("This"u8.ToArray()));
        reader.Advance(2);
        Assert.That(reader.Read(4), Is.EquivalentTo(new byte[][] { "This"u8.ToArray(), "Is"u8.ToArray(), "Test"u8.ToArray(), "Data"u8.ToArray() }));
        Assert.That(reader.IsAtEnd);
    }
}