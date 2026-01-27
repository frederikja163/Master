using System.Diagnostics;
using Master.Serializing.Encodings;

namespace Master.Serializing;

internal sealed class Serializer
{
    private readonly ILookup<LogicalType, IEncoding> _encodingsByType;
    private readonly Dictionary<EncodingId, IEncoding> _encodingsById;

    public Serializer():
        this(new SplitEncoding())
    {
        
    }
    
    public Serializer(params IEnumerable<IEncoding> encodings)
    {
        _encodingsById = encodings.ToDictionary(e => e.Id, e => e);
        _encodingsByType = _encodingsById.Values
            .SelectMany(e => e.GetSupportedTypes().Select(t => (t, e)))
            .ToLookup(t => t.t, t => t.e);
    }
    
    public int CascadingEncodings { get; init; } = 3;
    public double SamplePercentage { get; init; } = 0.1;
    public int SampleCount { get; init; } = 10;
    
    public MetadataColumn Encode(ReadOnlySpan<string> data)
    {
        Debug.Assert(data.Length > 0);
        
        ReadOnlySpan<string> sample = CreateSample(data);
        MetadataColumn metadataSample = PickEncoding(DataColumn.Create(sample), CascadingEncodings);
        DataColumn column = DataColumn.Create(data);
        return Encode(column, metadataSample);
    }
    
    public MetadataColumn Encode<T>(ReadOnlySpan<T> data) where T : struct
    {
        ReadOnlySpan<T> sample = CreateSample<T>(data);
        MetadataColumn metadataSample = PickEncoding(DataColumn.Create(sample), CascadingEncodings);
        DataColumn column = DataColumn.Create(data);
        return Encode(column, metadataSample);
    }

    public DataColumn Decode(MetadataColumn column)
    {
        if (column.Id == EncodingId.Binary)
        {
            return column.Metadata;
        }
        
        DataColumn[] physicalColumns = column.Columns.Select(c => Decode(c)).ToArray();
        return _encodingsById[column.Id].Decode(physicalColumns, column.Metadata);
    }

    private MetadataColumn Encode(DataColumn inData, MetadataColumn metadataSample)
    {
        EncodingId id = metadataSample.Id;

        if (id == EncodingId.Binary)
        {
            return new MetadataColumn(inData);
        }
        
        IEncoding encoding = _encodingsById[id];
        DataColumn metadata = metadataSample.Metadata;
        encoding.Encode(inData, ref metadata, out var columns);
        MetadataColumn[] column = columns
            .Zip(metadataSample.Columns)
            .Select(t => Encode(t.First, t.Second))
            .ToArray();
        return new MetadataColumn(id, metadata, column);
    }

    internal MetadataColumn PickEncoding(DataColumn sample, int cascades)
    {
        if (cascades == 0)
        {
            return new MetadataColumn(sample);
        }
        int minSize = sample.PhysicalSize;
        MetadataColumn bestEncoding = new MetadataColumn(sample);
        foreach (IEncoding encoding in _encodingsByType[sample.LogicalType])
        {
            DataColumn metadata = DataColumn.Empty;
            encoding.Encode(sample, ref metadata, out DataColumn[] columns);
            MetadataColumn[] childColumns = columns.Select(d => PickEncoding(d, cascades - 1)).ToArray();
            MetadataColumn metadataColumn = new MetadataColumn(encoding.Id, metadata, childColumns);
            int length = metadataColumn.CalculateTotalLength();
            if (length < minSize)
            {
                bestEncoding = metadataColumn;
                minSize = length;
            }
        }
        
        return bestEncoding;
    }

    internal ReadOnlySpan<T> CreateSample<T>(ReadOnlySpan<T> data)
    {
        int length = data.Length;
        if (length < SampleCount)
        {
            return data;
        }
        
        // Need to calculate sample length first, to round correctly.
        var sampleLength = (int)(length * SamplePercentage) / SampleCount;
        var totalSampleLength = sampleLength * SampleCount;
        Span<T> sample = new T[totalSampleLength];
        
        for (int i = 0; i < SampleCount; i++)
        {
            int startIndex = length / SampleCount * i;
            int endIndex = length / SampleCount * (i + 1);
            int index = Random.Shared.Next(startIndex, endIndex - sampleLength);
            data.Slice(index, sampleLength).CopyTo(sample.Slice(i * sampleLength, sampleLength));
        }

        return sample;
    }
}