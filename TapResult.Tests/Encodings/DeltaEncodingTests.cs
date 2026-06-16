using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;
using TapResult.Tests.Extensions;

namespace TapResult.Tests.Encodings;

public class DeltaEncodingTests
{
    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(50, 256)]
    [TestCase(1000, 10)]
    public void DeltaEncodingIncreasingRoundTripTest(int start, int length)
    {
        int[] data = Enumerable.Range(start, length).ToArray();
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(length).ToArray(), Is.EqualTo(data));
    }

    [TestCase(1, 5)]
    [TestCase(1, 10)]
    [TestCase(1, 1000)]
    [TestCase(42, 256)]
    [TestCase(1000, 10)]
    public void DeltaEncodingConstantRoundTripTest(int value, int repeats)
    {
        int[] data = Enumerable.Repeat(value, repeats).ToArray();
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(repeats).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingMixedValuesTest()
    {
        int[] data = [10, 20, 30, 25, 15, 5, 100, 200, 150];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingVerifiesDeltasTest()
    {
        int[] data = [10, 20, 30, 25, 15, 5, 100, 200, 150];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));

        int[] expectedDeltas = [10, 10, -5, -10, -10, 95, 100, -50];
        Assert.That(column.Deltas.OpenReader<int>().Read(expectedDeltas.Length).ToArray(), Is.EqualTo(expectedDeltas));
    }

    [Test]
    public void DeltaEncodingSingleValueTest()
    {
        int[] data = [42];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(1));
        Assert.That(reader.Read(), Is.EqualTo(42));
        Assert.That(column.Deltas.LogicalLength, Is.EqualTo(0));
    }

    [Test]
    public void DeltaEncodingEmptyTest()
    {
        int[] data = [];
        DataColumn dataColumn = new DataColumn(LogicalType.SInt32, ReadOnlyMemory<byte>.Empty, 0);
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));

        Assert.That(column.LogicalLength, Is.EqualTo(0));
    }

    [Test]
    public void DeltaEncodingDecreasingTest()
    {
        int[] data = [100, 90, 80, 70, 60];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForFloatTest()
    {
        float[] data = [1.5f, 2.5f, 3.5f, 2.0f, 0.5f, 10.0f, 20.0f, 15.0f];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<float> reader = Assert.InstanceOf<DeltaColumnReader<float>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForDoubleTest()
    {
        double[] data = [1.5, 2.5, 3.5, 2.0, 0.5, 10.0, 20.0, 15.0];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<double> reader = Assert.InstanceOf<DeltaColumnReader<double>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForUnsignedIntTest()
    {
        uint[] data = [10u, 20u, 30u, 25u, 15u, 5u, 100u, 200u, 150u];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<uint> reader = Assert.InstanceOf<DeltaColumnReader<uint>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingPeekTest()
    {
        int[] data = [5, 10, 15, 20, 25];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Peek(), Is.EqualTo(5));
        Assert.That(reader.Peek(2), Is.EqualTo(15));
        Assert.That(reader.Peek(4), Is.EqualTo(25));
    }

    [Test]
    public void DeltaEncodingAdvanceAndPeekTest()
    {
        int[] data = [5, 10, 15, 20, 25];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        reader.Advance(2);
        Assert.That(reader.Index, Is.EqualTo(2));
        Assert.That(reader.Peek(), Is.EqualTo(15));
        Assert.That(reader.Peek(2), Is.EqualTo(25));
    }

    [Test]
    public void DeltaEncodingCloneTest()
    {
        int[] data = [5, 10, 15, 20, 25];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        reader.Advance(2);
        DeltaColumnReader<int> clone = Assert.InstanceOf<DeltaColumnReader<int>>(reader.Clone());

        Assert.That(clone.Index, Is.EqualTo(2));
        Assert.That(clone.Peek(), Is.EqualTo(15));

        reader.Advance(1);
        clone.Advance(2);

        Assert.That(reader.Peek(), Is.EqualTo(20));
        Assert.That(clone.Peek(), Is.EqualTo(25));
    }

    [Test]
    public void DeltaEncodingBulkPeekTest()
    {
        int[] data = [5, 10, 15, 20, 25];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<int> reader = Assert.InstanceOf<DeltaColumnReader<int>>(column.OpenReader());

        reader.Advance(1);
        Assert.That(reader.Peek(0, 3).ToArray(), Is.EqualTo(new[] { 10, 15, 20 }));
    }

    [Test]
    public void DeltaEncodingForSignedByteTest()
    {
        sbyte[] data = [5, 10, 15, 12, 8, 3];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<sbyte>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<sbyte> reader = Assert.InstanceOf<DeltaColumnReader<sbyte>>(column.OpenReader());

        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForShortTest()
    {
        short[] data = [100, 200, 300, 150, 50];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<short>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<short> reader = Assert.InstanceOf<DeltaColumnReader<short>>(column.OpenReader());

        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForLongTest()
    {
        long[] data = [100000, 200000, 300000, 150000, 50000];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<long>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<long> reader = Assert.InstanceOf<DeltaColumnReader<long>>(column.OpenReader());

        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForUnsignedShortTest()
    {
        ushort[] data = [10, 20, 30, 25, 15];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<ushort>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<ushort> reader = Assert.InstanceOf<DeltaColumnReader<ushort>>(column.OpenReader());

        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingForUnsignedLongTest()
    {
        ulong[] data = [10, 20, 30, 25, 15];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<ulong>(data.AsSpan()));
        IEncoding encoding = new DeltaEncoding();
        DeltaColumn column = Assert.InstanceOf<DeltaColumn>(encoding.Encode(dataColumn));
        DeltaColumnReader<ulong> reader = Assert.InstanceOf<DeltaColumnReader<ulong>>(column.OpenReader());

        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DeltaEncodingThroughEncoderTest()
    {
        int[] data = [10, 20, 30, 25, 15, 5, 100, 200, 150];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        Encoder encoder = new Encoder()
        {
            CascadingEncodings = 1,
            SampleCount = 1,
            SamplePercentage = 1.0,
        };
        IColumn column = encoder.Encode(dataColumn);

        int[] read = column.OpenReader<int>().Read(data.Length).ToArray();
        Assert.That(read, Is.EqualTo(data));
    }
}
