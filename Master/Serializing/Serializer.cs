using System.Diagnostics;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Serializing;

public sealed class Serializer
{
    private readonly ILookup<LogicalType, IEncoding> _encodingsByType;
    private readonly Dictionary<EncodingId, IEncoding> _encodingsById;

    public Serializer():
        this(new SplitEncoding(), new BitPacking())
    {
        
    }
    
    public Serializer(params IEnumerable<IEncoding> encodings)
    {
        _encodingsById = encodings.ToDictionary(e => e.Id, e => e);
        _encodingsByType = _encodingsById.Values
            .SelectMany(e => e.GetSupportedTypes().Select(t => (t, e)))
            .ToLookup(t => t.t, t => t.e);
    }
    
    public int CascadingEncodings { get; init; } = 2;
    public double SamplePercentage { get; init; } = 0.1;
    public int SampleCount { get; init; } = 10;
    public int MaxSampleLength = 1024;
    
    public IColumn Encode(DataColumn column)
    {
        DataColumn sample = CreateSample(column);
        IColumn metadataSample = PickEncoding(sample, CascadingEncodings);
        return Encode(column, metadataSample);
    }

    private IColumn Encode(DataColumn inData, IColumn metadataSample)
    {
        if (metadataSample is DataColumn col)
        {
            return inData;
        }
        EncodingId id = metadataSample.Id;
        
        IEncoding encoding = _encodingsById[id];
        var columns = encoding.Encode(inData);
        if (columns is IColumnParent parent && metadataSample is IColumnParent parentMeta)
        {
            foreach (var child in columns.GetDataColumns().Zip(parentMeta.GetChildColumns()))
            {
                parent.Swap(child.First, Encode(child.First, child.Second));
            }
        }
        return columns;
    }

    internal IColumn PickEncoding(DataColumn sample, int cascades)
    {
        if (cascades == 0)
        {
            return sample;
        }
        int minSize = sample.PhysicalSize / 4 * 3;
        IColumn bestEncoding = sample;
        if (!_encodingsByType.Contains(sample.LogicalType))
        {
            return sample;
        }
        
        foreach (IEncoding encoding in _encodingsByType[sample.LogicalType])
        {
            IColumn encodedColumn = encoding.Encode(sample);
            if (encodedColumn is IColumnParent parent)
            {
                foreach (var child in encodedColumn.GetDataColumns())
                {
                    parent.Swap(child, PickEncoding(child, cascades - 1));
                }
            }
            int length = encodedColumn.CalculateTotalLength();
            if (length < minSize)
            {
                bestEncoding = encodedColumn;
                minSize = length;
            }
        }
        
        return bestEncoding;
    }

    internal DataColumn CreateSample(DataColumn data)
    {
        int length = data.LogicalLength;
        if (length < SampleCount)
        {
            return data;
        }
        
        // Need to calculate sample length first, to round correctly.
        var sampleLength = (int)(length * SamplePercentage) / SampleCount;
        sampleLength = Math.Min(sampleLength, MaxSampleLength);
        var totalSampleLength = sampleLength * SampleCount;
        int size = data.LogicalType.TryGetSize(out int s) ? s : 1;
        DataColumnBuilder builder = new DataColumnBuilder(data.LogicalType, totalSampleLength * size, false);
        DataColumnReader<byte> reader = data.OpenDataColumnReader<byte>();
        
        int sectionLength = length / SampleCount;
        for (int i = 0; i < SampleCount; i++)
        {
            int index = Random.Shared.Next(0, sectionLength - sampleLength);
            reader.Advance(index);
            builder.WriteRaw(reader.ReadUnits(sampleLength), sampleLength);
            reader.Advance(sectionLength - index - sampleLength);
        }

        return builder.Build();
    }
}