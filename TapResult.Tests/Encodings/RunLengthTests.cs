using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Tests.Encodings;

public class RunLengthTests
{
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1, 256)]
    [TestCase(1000, 10)]
    public void RunLengthEncodingRoundTripTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn dataColumn = DataColumn.Create<int>(data.AsSpan());
        IEncoding encoding = new RunLengthEncoding();
        IColumn column = encoding.Encode(dataColumn);
        DataColumnBuilder metadataBuilder = new DataColumnBuilder(BitPackingColumn.Size + Unsafe.SizeOf<int>());
        column.WriteMetadata(ref metadataBuilder);
        DataColumn metadataColumn = metadataBuilder.Build();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<int> reader = (IColumnReader<int>)encoding.CreateDecoder(
            LogicalType.SInt32,
            ref genericReader, column.GetDataColumns().Select(c => c.OpenReader()));
        
        RunLengthColumn rleColumn = (RunLengthColumn) column;
        RunLengthReader<int> runLengthReader = (RunLengthReader<int>) reader;
        Assert.That(runLengthReader.Length, Is.EqualTo(rleColumn.Length));
        Assert.That(runLengthReader.ByteLength, Is.EqualTo(rleColumn.ByteLength));
        Assert.That(reader.Read(length).ToArray(), Is.EqualTo(data));
    }
    
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1, 256)]
    [TestCase(1000, 10)]
    public void RunLengthEncodingRepeatingValuesRoundTripTest(int value, int repeats)
    {
        int[] data = Enumerable.Repeat(value, repeats).ToArray();
        DataColumn dataColumn = DataColumn.Create<int>(data.AsSpan());
        IEncoding encoding = new RunLengthEncoding();
        IColumn column = encoding.Encode(dataColumn);
        DataColumnBuilder metadataBuilder = new DataColumnBuilder(BitPackingColumn.Size + Unsafe.SizeOf<int>());
        column.WriteMetadata(ref metadataBuilder);
        DataColumn metadataColumn = metadataBuilder.Build();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<int> reader = (IColumnReader<int>)encoding.CreateDecoder(
            LogicalType.SInt32,
            ref genericReader, column.GetDataColumns().Select(c => c.OpenReader()));
        
        RunLengthColumn rleColumn = (RunLengthColumn) column;
        RunLengthReader<int> runLengthReader = (RunLengthReader<int>) reader;
        Assert.That(rleColumn.ByteColumn.CalculateTotalLength(), Is.EqualTo(1));
        Assert.That(rleColumn.RepeatColumn.CalculateTotalLength(), Is.EqualTo(1));
        Assert.That(((DataColumn)rleColumn.RepeatColumn).OpenReader<int>().Read(), Is.EqualTo(repeats));
        Assert.That(((DataColumn)rleColumn.ByteColumn).OpenReader<int>().Read(), Is.EqualTo(value));
        Assert.That(runLengthReader.Length, Is.EqualTo(rleColumn.Length));
        Assert.That(runLengthReader.ByteLength, Is.EqualTo(rleColumn.ByteLength));
        Assert.That(reader.Read(repeats).ToArray(), Is.EqualTo(data));
    }
    
    public void TestRunLengthEncodingTest()
    {
        int[] data = [1,1,1,1,1,1,1, 5,5,5,5,5, 1,1,1,1,1, 3,3,3,3,3,3];
        DataColumn dataColumn = DataColumn.Create(data.AsSpan());
        IEncoding encoding = new RunLengthEncoding();
        IColumn column = encoding.Encode(dataColumn);
        DataColumnBuilder metadataBuilder = new DataColumnBuilder(BitPackingColumn.Size + Unsafe.SizeOf<int>());
        column.WriteMetadata(ref metadataBuilder);
        DataColumn metadataColumn = metadataBuilder.Build();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<int> reader = (IColumnReader<int>)encoding.CreateDecoder(
            LogicalType.SInt32,
            ref genericReader, column.GetDataColumns().Select(c => c.OpenReader()));
        
        RunLengthColumn rleColumn = (RunLengthColumn) column;
        RunLengthReader<int> runLengthReader = (RunLengthReader<int>) reader;
        Assert.That(rleColumn.ByteColumn.CalculateTotalLength(), Is.EqualTo(4));
        Assert.That(rleColumn.RepeatColumn.CalculateTotalLength(), Is.EqualTo(4));
        Assert.That(((DataColumn)rleColumn.RepeatColumn).OpenReader<int>().Read(4), Is.EqualTo(new[] {7, 5, 5, 6}));
        Assert.That(((DataColumn)rleColumn.ByteColumn).OpenReader<int>().Read(4), Is.EqualTo(new[] {1, 5, 1, 3}));
        Assert.That(runLengthReader.Length, Is.EqualTo(rleColumn.Length));
        Assert.That(runLengthReader.ByteLength, Is.EqualTo(rleColumn.ByteLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }
}