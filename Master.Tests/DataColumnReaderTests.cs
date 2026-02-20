using Master.Serializing;
using Master.Serializing.Columns;

namespace Master.Tests;

internal sealed class DataColumnReaderTests
{
    [Test]
    public void AdvanceTest()
    {
        DataColumn column = DataColumn.Create(Enumerable.Range(0, 100).ToArray());
        DataColumnReader reader = column.OpenReader();
        reader.Advance(12);
        Assert.That(reader.Read<int>(), Is.EqualTo(3));
        reader.AdvanceUnits(40);
        Assert.That(reader.Read<int>(), Is.EqualTo(44));
    }

    [Test]
    public void OverFlowTest()
    {
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader().Advance(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader().AdvanceUnits(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader().Peek<int>(1));
        Assert.Throws<IndexOutOfRangeException>(() => DataColumn.Empty.OpenReader().Read<int>());
    }
    
    [Test]
    public void ReadPrimitiveTest()
    {
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.SInt32, 405, false);
        builder.Write<byte>(123);
        builder.Write(123);
        builder.Write(Enumerable.Range(0, 100).ToArray());
        DataColumnReader reader = builder.Build().OpenReader();
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
        DataColumnReader reader = column.OpenReader();
        Assert.That(reader.ReadUnits(3).ToArray(),
            Is.EquivalentTo(column.Data.Span.Slice(0, 36).ToArray()));
    }

    [Test]
    public void ReadMultiplePrimitivesTest()
    {
        float[] data = Enumerable.Range(0, 100).Select(i => MathF.Sin(i / 10f)).ToArray();
        DataColumn column = DataColumn.Create(data);
        DataColumnReader reader = column.OpenReader();
        
        Assert.That(reader.Read<float>(40).ToArray(), Is.EquivalentTo(data.Take(40)));
    }

    [Test]
    public void ReadStringsTests()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        DataColumn column = DataColumn.Create(strings);
        DataColumnReader reader = column.OpenReader();

        Assert.That(reader.ReadString(), Is.EqualTo("test"));
        reader.AdvanceUnits(2);
        Assert.That(reader.ReadString(4), Is.EquivalentTo(new string[] { "This", "Is", "Test", "Data" }));
        Assert.That(reader.AtEnd);
    }

    [Test]
    public void ReadBlobsTest()
    {
        string[] strings = ["test", "hello world", "i am here", "This", "Is", "Test", "Data"];
        DataColumn column = DataColumn.Create(strings);
        DataColumnReader reader = column.OpenReader();

        Assert.That(reader.ReadBlob().ToArray(), Is.EqualTo("test"u8.ToArray()));
        reader.AdvanceUnits(2);
        Assert.That(reader.ReadBlob(4), Is.EquivalentTo(new byte[][] { "This"u8.ToArray(), "Is"u8.ToArray(), "Test"u8.ToArray(), "Data"u8.ToArray() }));
        Assert.That(reader.AtEnd);
    }
}