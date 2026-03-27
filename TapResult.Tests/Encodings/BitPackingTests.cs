using System.Runtime.CompilerServices;
using TapResult;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Tests.Encodings;

internal sealed class BitPackingTests
{
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1, 256)]
    [TestCase(1000, 10)]
    public void BitPackEncodingRoundTripTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn dataColumn = ColumnBuilder.Create<int>(data.AsSpan());
        IEncoding encoding = new BitPacking();
        BitPackingColumn column = (BitPackingColumn)encoding.Encode(dataColumn);
        ColumnBuilder metadataBuilder = new ColumnBuilder(BitPackingColumn.Size + Unsafe.SizeOf<int>());
        column.WriteMetadata(metadataBuilder);
        DataColumn metadataColumn = metadataBuilder.BuildDataColumn();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<int> reader = (IColumnReader<int>)encoding.CreateDecoder(
            LogicalType.SInt32,
            genericReader, ((DataColumn)column.Column).OpenReader<int>());
        
        Assert.That(reader.Read(length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void GetBitCountsTest()
    {
        Span<int> bitCounts = stackalloc int[sizeof(ulong) * 8];
        BitPacking.GetBitCounts<byte>(ColumnBuilder.Create<byte>(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()),
            bitCounts);
        Assert.That(bitCounts.Slice(0, 8).ToArray(), Is.EqualTo(new int[8] {21, 0, 0, 5, 8, 9, 10, 10 }));
        Assert.That(bitCounts.Slice(8).ToArray().All(i => i == 0), Is.True);
    }

    [Test]
    public void GetMetadataTest()
    {
        BitPackingColumn metadata = BitPacking.GetMetadata<byte>(ColumnBuilder.Create<byte>(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()));
        Assert.That(metadata.LogicalLength, Is.EqualTo(21));
        Assert.That(metadata.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(metadata.Prefix, Is.EqualTo(0b100));
        Assert.That(metadata.PrefixLength, Is.EqualTo(3));
    }
}