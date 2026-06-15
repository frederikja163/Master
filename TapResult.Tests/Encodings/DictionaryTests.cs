using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;
using TapResult.Tests.Extensions;

namespace TapResult.Tests.Encodings;

public class DictionaryTests
{
    [Test]
    public void DictionaryEncodingRoundTripTest()
    {
        int[] data = [1, 5, 3, 1, 5, 7, 1, 3, 9, 5];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new DictionaryEncoding();
        DictionaryColumn column = Assert.InstanceOf<DictionaryColumn>(encoding.Encode(dataColumn));
        DictionaryColumnReader<int> reader = Assert.InstanceOf<DictionaryColumnReader<int>>(column.OpenReader());

        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DictionaryEncodingAllUniqueTest()
    {
        int[] data = Enumerable.Range(1, 100).ToArray();
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new DictionaryEncoding();
        DictionaryColumn column = Assert.InstanceOf<DictionaryColumn>(encoding.Encode(dataColumn));
        DictionaryColumnReader<int> reader = Assert.InstanceOf<DictionaryColumnReader<int>>(column.OpenReader());

        Assert.That(column.ValuesColumn.OpenReader<int>().Read(data.Length), Is.EqualTo(data));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DictionaryEncodingWithRepeatsTest()
    {
        int[] data = [1, 1, 1, 1, 5, 5, 5, 5, 1, 1, 1, 1];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new DictionaryEncoding();
        DictionaryColumn column = Assert.InstanceOf<DictionaryColumn>(encoding.Encode(dataColumn));
        DictionaryColumnReader<int> reader = Assert.InstanceOf<DictionaryColumnReader<int>>(column.OpenReader());

        Assert.That(column.ValuesColumn.OpenReader<int>().Read(2), Is.EqualTo(new[] { 1, 5 }));
        Assert.That(column.IndexColumn.OpenReader<int>().Read(12), Is.EqualTo(new[] { 0, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0 }));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DictionaryEncodingSingleValueTest()
    {
        int[] data = Enumerable.Repeat(42, 50).ToArray();
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new DictionaryEncoding();
        DictionaryColumn column = Assert.InstanceOf<DictionaryColumn>(encoding.Encode(dataColumn));
        DictionaryColumnReader<int> reader = Assert.InstanceOf<DictionaryColumnReader<int>>(column.OpenReader());

        Assert.That(column.ValuesColumn.OpenReader<int>().Read(1), Is.EqualTo(new[] { 42 }));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }

    [Test]
    public void DictionaryEncodingForFloatTest()
    {
        float[] data = [1.1f, 2.2f, 3.3f, 1.1f, 2.2f, 3.3f, 1.1f];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new DictionaryEncoding();
        DictionaryColumn column = Assert.InstanceOf<DictionaryColumn>(encoding.Encode(dataColumn));
        DictionaryColumnReader<float> reader = Assert.InstanceOf<DictionaryColumnReader<float>>(column.OpenReader());

        Assert.That(column.ValuesColumn.OpenReader<float>().Read(3), Is.EqualTo(new[] { 1.1f, 2.2f, 3.3f }));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }
}
