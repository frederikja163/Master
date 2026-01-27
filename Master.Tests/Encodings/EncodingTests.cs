using System.Text;
using Master.Serializing;
using Master.Serializing.Encodings;

namespace Master.Tests.Encodings;

internal sealed class EncodingTests
{
    [TestCase(1, 10, 0.2)]
    [TestCase(100, 10, 0.2)]
    [TestCase(100, 10, 0.1)]
    [TestCase(128, 5, 0.1)]
    [TestCase(1024, 10, 0.1)]
    public void SampleIsCorrectTest(int dataSize, int sampleCount, double samplePercentage)
    {
        Serializer serializer = new Serializer()
        {
            SampleCount = sampleCount,
            SamplePercentage = samplePercentage,
        };
        int[] data = Enumerable.Range(0, dataSize).ToArray();
        ReadOnlySpan<int> sample = serializer.CreateSample(data);

        int sampleLength = (int)(dataSize * samplePercentage) / sampleCount;
        int totalSampleLength = sampleLength * sampleCount;
        totalSampleLength = Math.Max(totalSampleLength, 1);
        
        Assert.That(sample.Length, Is.EqualTo(totalSampleLength));
        sampleCount = Math.Min(sampleCount, totalSampleLength);

        for (int i = 0; i < sampleCount; i++)
        {
            int prevValue = sample[i * sampleLength];
            for (int j = 1; j < sampleLength; j++)
            {
                int index = j + i * sampleLength;
                int value = sample[index];
                Assert.That(value, Is.EqualTo(prevValue + 1));
                prevValue = value;
            }
        }
    }

    [Test]
    public void SplitStringEncodingTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        SplitEncoding encoder = new SplitEncoding();
        DataColumn metadata = DataColumn.Empty;
        encoder.Encode(DataColumn.Create(strs.AsSpan()), ref metadata, out var columns);
        Assert.That(columns.Length, Is.EqualTo(2));
        Assert.That(metadata.LogicalLength, Is.EqualTo(4));
        DataColumn lengthColumn = columns[0];
        DataColumnReader lengthReader = lengthColumn.OpenReader();
        DataColumn strColumn = columns[1];
        DataColumnReader strReader = strColumn.OpenReader();
        Assert.That(lengthColumn.LogicalType, Is.EqualTo(LogicalType.SInt32));
        Assert.That(lengthColumn.LogicalLength, Is.EqualTo(2));
        Assert.That(lengthReader.Read<int>(2).ToArray(), Is.EquivalentTo(new int[]{str1.Length, str2.Length}));
        Assert.That(strColumn.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(strColumn.LogicalLength, Is.EqualTo(str1.Length + str2.Length));
        Assert.That(strReader.Read<byte>(str1.Length + str2.Length).ToArray(), Is.EquivalentTo(Encoding.UTF8.GetBytes(str1).Concat(Encoding.UTF8.GetBytes(str2))));
    }

    [Test]
    public void SplitStringEncodingRoundRobinTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        DataColumn metadata = DataColumn.Empty;
        SplitEncoding encoding = new SplitEncoding();
        encoding.Encode(DataColumn.Create(strs.AsSpan()), ref metadata, out DataColumn[] columns);
        DataColumnReader decoded = encoding.Decode(columns, metadata).OpenReader();
        Assert.That(decoded.ReadString(2), Is.EquivalentTo(strs));
    }
}