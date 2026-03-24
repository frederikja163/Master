using TapResult;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Tests.Encodings;

internal sealed class EncodingTests
{
    [TestCase(1, 10, 0.2)]
    [TestCase(100, 10, 0.2)]
    [TestCase(100, 10, 0.1)]
    [TestCase(128, 5, 0.1)]
    [TestCase(1024, 10, 0.1)]
    public void SampleIsCorrectTest(int dataSize, int sampleCount, double samplePercentage)
    {
        Encoder encoder = new Encoder()
        {
            SampleCount = sampleCount,
            SamplePercentage = samplePercentage,
        };
        int[] data = Enumerable.Range(0, dataSize).ToArray();
        DataColumn sample = encoder.CreateSample(DataColumn.Create<int>(data));

        int sampleLength = (int)(dataSize * samplePercentage) / sampleCount;
        int totalSampleLength = sampleLength * sampleCount;
        totalSampleLength = Math.Max(totalSampleLength, 1);
        
        Assert.That(sample.LogicalLength, Is.EqualTo(totalSampleLength));
        sampleCount = Math.Min(sampleCount, totalSampleLength);

        IColumnReader<int> reader = sample.OpenReader<int>();
        for (int i = 0; i < sampleCount; i++)
        {
            int prevValue = reader.Read();
            for (int j = 1; j < sampleLength; j++)
            {
                int value = reader.Read();
                Assert.That(value, Is.EqualTo(prevValue + 1));
                prevValue = value;
            }
        }
    }
    
    [Test]
    public void StringRoundtripTest()
    {
        string[] data = ["test", "testing", "hello", "world"];
        
        Encoder encoder = new Encoder()
        {
            SampleCount = 1,
            SamplePercentage = 0.5,
            CascadingEncodings = 2,
        };

        DataColumn expected = DataColumn.Create(data);
        IColumn column = encoder.Encode(expected);
        // TODO: Read
        // DataColumnBuilder builder = new DataColumnBuilder(100);
        // column.WriteMetadata(builder);
        // DataColumn dataColumn = serializer.Decode(column);
        //
        // Assert.That(dataColumn.Data.ToArray(), Is.EqualTo(expected.Data.ToArray()));
    }
    
    [Test]
    public void IntRoundtripTest()
    {
        int[] data = [1,2,3,0];
        
        Encoder encoder = new Encoder();

        DataColumn expected = DataColumn.Create<int>(data);
        IColumn column = encoder.Encode(expected);
        //TODO: read
        //DataColumn dataColumn = serializer.Decode(column);
        //Assert.That(dataColumn.Data.ToArray(), Is.EqualTo(expected.Data.ToArray()));
    }
    
    [Test]
    public void UIntRoundtripTest()
    {
        uint[] data = [1,2,3,0];
        
        Encoder encoder = new Encoder();

        DataColumn expected = DataColumn.Create<uint>(data);
        IColumn column = encoder.Encode(expected);
        //TODO: read
        //DataColumn dataColumn = serializer.Decode(column);
        //Assert.That(dataColumn.Data.ToArray(), Is.EqualTo(expected.Data.ToArray()));
    }
}