using Master.Serializing;
using Master.Serializing.Encodings;

namespace Master.Tests.Encodings;

internal sealed class BitPackingTests
{
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(1000, 10)]
    public void BitPackEncodingTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn dataColumn = DataColumn.Create(data.AsSpan());
        DataColumn metadata = DataColumn.Empty;
        IEncoding encoding = new BitPacking();
        encoding.Encode(dataColumn, ref metadata, out DataColumn[] columns);
        DataColumnReader decoded = encoding.Decode(columns, metadata).OpenReader();
        Assert.That(decoded.Read<int>(length).ToArray(), Is.EquivalentTo(data));
    }

    [Test]
    public void GetBitCountsTest()
    {
        Span<int> bitCounts = stackalloc int[sizeof(ulong) * 8];
        BitPacking.GetBitCounts<byte>(DataColumn.Create(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()),
            bitCounts);
        Assert.That(bitCounts.Slice(0, 8).ToArray(), Is.EquivalentTo(new int[8] {21, 0, 0, 5, 8, 9, 10, 10 }));
        Assert.That(bitCounts.Slice(8).ToArray().All(i => i == 0), Is.True);
    }

    [Test]
    public void GetMetadataTest()
    {
        DataColumn metadataCol = DataColumn.Empty;
        BitPacking.Metadata metadata = BitPacking.GetMetadata<byte>(DataColumn.Create(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()), ref metadataCol);
        Assert.That(new BitPacking.Metadata(metadataCol), Is.EqualTo(metadata));
        Assert.That(metadata.LogicalLength, Is.EqualTo(21));
        Assert.That(metadata.Type, Is.EqualTo(LogicalType.UInt8));
        Assert.That(metadata.Prefix, Is.EqualTo(0b100));
        Assert.That(metadata.PrefixLength, Is.EqualTo(3));
    }
}