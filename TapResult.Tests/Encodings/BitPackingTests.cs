using System.Runtime.CompilerServices;
using NUnit.Framework.Constraints;
using TapResult;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;
using TapResult.Tests.Extensions;

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
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new BitPacking();
        BitPackingColumn column = Assert.InstanceOf<BitPackingColumn>(encoding.Encode(dataColumn));
        IColumnReader<int> reader = column.OpenReader<int>();
        
        Assert.That(reader.Read(length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void GetBitCountsTest()
    {
        Span<int> bitCounts = stackalloc int[sizeof(ulong) * 8];
        DataColumn column = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<byte>(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()));
        BitPacking.GetBitCounts<byte>(column.OpenReader<byte>(),
            bitCounts);
        Assert.That(bitCounts.Slice(0, 8).ToArray(), Is.EqualTo(new int[8] {21, 0, 0, 5, 8, 9, 10, 10 }));
        Assert.That(bitCounts.Slice(8).ToArray().All(i => i == 0), Is.True);
    }

    [Test]
    public void GetMetadataTest()
    {
        DataColumn column = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<byte>(Enumerable.Range(128, 21).Select(i => (byte)i).ToArray().AsSpan()));
        BitPacking.GetMetadata<byte>(column.OpenReader<byte>(), out byte prefixLength, out ulong prefix);
        Assert.That(prefix, Is.EqualTo(0b100));
        Assert.That(prefixLength, Is.EqualTo(3));
    }
}