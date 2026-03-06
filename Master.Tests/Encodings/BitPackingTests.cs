using System.Runtime.CompilerServices;
using Master.Serializing;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;
using Master.Serializing.Readers;

namespace Master.Tests.Encodings;

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
        DataColumn dataColumn = DataColumn.Create<int>(data.AsSpan());
        IEncoding encoding = new BitPacking();
        IColumn columns = encoding.Encode(dataColumn);
        DataColumnBuilder metadataBuilder = new DataColumnBuilder(BitPackingColumn.Size + Unsafe.SizeOf<int>());
        columns.WriteMetadata(ref metadataBuilder);
        DataColumn metadataColumn = metadataBuilder.Build();
        GenericReader genericReader = metadataColumn.OpenGenericReader();
        IColumnReader<int> reader = (IColumnReader<int>)encoding.CreateDecoder(
            LogicalType.SInt32,
            ref genericReader, columns.GetDataColumns().FirstOrDefault().OpenReader<int>());
        
        Assert.That(reader.Read(length).ToArray(), Is.EquivalentTo(data));
    }

    [Test]
    public void GetBitCountsTest()
    {
        Span<int> bitCounts = stackalloc int[sizeof(ulong) * 8];
        BitPacking.GetBitCounts<byte>(DataColumn.Create<byte>(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()),
            bitCounts);
        Assert.That(bitCounts.Slice(0, 8).ToArray(), Is.EquivalentTo(new int[8] {21, 0, 0, 5, 8, 9, 10, 10 }));
        Assert.That(bitCounts.Slice(8).ToArray().All(i => i == 0), Is.True);
    }

    [Test]
    public void GetMetadataTest()
    {
        BitPackingColumn metadata = BitPacking.GetMetadata<byte>(DataColumn.Create<byte>(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()));
        Assert.That(metadata.LogicalLength, Is.EqualTo(21));
        Assert.That(metadata.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(metadata.Prefix, Is.EqualTo(0b100));
        Assert.That(metadata.PrefixLength, Is.EqualTo(3));
    }
}