using System.Runtime.CompilerServices;
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
}