using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;
using TapResult.Tests.Extensions;

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
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new RunLengthEncoding();
        RunLengthColumn column = Assert.InstanceOf<RunLengthColumn>(encoding.Encode(dataColumn));
        RunLengthReader<int> reader = Assert.InstanceOf<RunLengthReader<int>>(column.OpenReader());
        
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
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
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create<int>(data.AsSpan()));
        IEncoding encoding = new RunLengthEncoding();
        RunLengthColumn column = Assert.InstanceOf<RunLengthColumn>(encoding.Encode(dataColumn));
        RunLengthReader<int> reader = Assert.InstanceOf<RunLengthReader<int>>(column.OpenReader());
        
        Assert.That(column.RepeatColumn.OpenReader<int>().Read(), Is.EqualTo(repeats));
        Assert.That(column.ByteColumn.OpenReader<int>().Read(), Is.EqualTo(value));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(repeats).ToArray(), Is.EqualTo(data));
    }
    
    [Test]
    public void TestRunLengthEncodingTest()
    {
        int[] data = [1,1,1,1,1,1,1, 5,5,5,5,5, 1,1,1,1,1, 3,3,3,3,3,3];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new RunLengthEncoding();
        RunLengthColumn column = Assert.InstanceOf<RunLengthColumn>(encoding.Encode(dataColumn));
        RunLengthReader<int> reader = Assert.InstanceOf<RunLengthReader<int>>(column.OpenReader());
        
        Assert.That(column.RepeatColumn.OpenReader<int>().Read(4), Is.EqualTo(new[] {7, 5, 5, 6}));
        Assert.That(column.ByteColumn.OpenReader<int>().Read(4), Is.EqualTo(new[] {1, 5, 1, 3}));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }
    
    [Test]
    public void TestRunLengthEncodingForFloatTest()
    {
        float[] data = [1.1f,1.1f,1.1f,1.1f,1.1f,1.1f,1.1f, 5.5f,5.5f,5.5f,5.5f,5.5f, 1.2f,1.2f, 1.3f,1.3f,1.3f, 3.4f,3.4f,3.4f,3.4f,3.4f,3.4f];
        DataColumn dataColumn = Assert.InstanceOf<DataColumn>(ColumnBuilder.Create(data.AsSpan()));
        IEncoding encoding = new RunLengthEncoding();
        RunLengthColumn column = Assert.InstanceOf<RunLengthColumn>(encoding.Encode(dataColumn));
        RunLengthReader<float> reader = Assert.InstanceOf<RunLengthReader<float>>(column.OpenReader());
        
        Assert.That(column.RepeatColumn.OpenReader<int>().Read(5), Is.EqualTo(new[] {7, 5, 2, 3, 6}));
        Assert.That(column.ByteColumn.OpenReader<float>().Read(5), Is.EqualTo(new[] {1.1f, 5.5f, 1.2f, 1.3f, 3.4f}));
        Assert.That(reader.Length, Is.EqualTo(column.LogicalLength));
        Assert.That(reader.Read(data.Length).ToArray(), Is.EqualTo(data));
    }
}