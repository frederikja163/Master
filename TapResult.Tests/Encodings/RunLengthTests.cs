using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Tests;

public class RunLengthTests
{
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1, 256)]
    [TestCase(1000, 10)]
    public void BitPackEncodingRoundTripTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn dataColumn = DataColumn.Create<int>(data.AsSpan());
        IEncoding encoding = new RunLengthEncoding();
        IColumn columns = encoding.Encode(dataColumn);
        DataColumnBuilder metadataBuilder = new DataColumnBuilder(BitPackingColumn.Size + Unsafe.SizeOf<int>());
        columns.WriteMetadata(ref metadataBuilder);
        DataColumn metadataColumn = metadataBuilder.Build();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<int> reader = (IColumnReader<int>)encoding.CreateDecoder(
            LogicalType.SInt32,
            ref genericReader, columns.GetDataColumns().Select(c => c.OpenReader()));
        
        Assert.That(reader.Read(length).ToArray(), Is.EqualTo(data));
    }
}