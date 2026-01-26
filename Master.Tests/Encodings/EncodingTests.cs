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
        Column output = encoder.Encode(PhysicalColumn.Create(strs.AsSpan()));
        Assert.That(output.PhysicalColumns.Length, Is.EqualTo(2));
        Assert.That(output.Parameters.Length, Is.EqualTo(4));
        PhysicalColumn lengthColumn = output.PhysicalColumns[0];
        PhysicalColumn strColumn = output.PhysicalColumns[1];
        Assert.That(lengthColumn.LogicalType, Is.EqualTo(LogicalType.SInt32));
        Assert.That(lengthColumn.LogicalLength, Is.EqualTo(2));
        Assert.That(lengthColumn.Interpret<int>().ToArray(), Is.EquivalentTo(new int[]{str1.Length, str2.Length}));
        Assert.That(strColumn.LogicalType, Is.EqualTo(LogicalType.UInt8));
        Assert.That(strColumn.LogicalLength, Is.EqualTo(str1.Length + str2.Length));
        Assert.That(strColumn.Interpret<byte>().ToArray(), Is.EquivalentTo(Encoding.UTF8.GetBytes(str1).Concat(Encoding.UTF8.GetBytes(str2))));
    }

    [Test]
    public void SplitStringEncodingRoundRobinTest()
    {
        string str1 = "Hello world";
        string str2 = "testing1234";
        string[] strs = [str1, str2];
        Serializer serializer = new Serializer(new SplitEncoding())
        {
            CascadingEncodings = 1
        };
        EncodedColumn encoded = serializer.Encode(strs);
        PhysicalColumn decoded = serializer.Decode(encoded);
    }
}