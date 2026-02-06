using System.Diagnostics;
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
    
    public int CascadingEncodings { get; init; } = 3;
    public double SamplePercentage { get; init; } = 0.1;
    public int SampleCount { get; init; } = 10;
    
    public MetadataColumn Encode(DataColumn column)
    {
        DataColumn sample = CreateSample(column);
        MetadataColumn metadataSample = PickEncoding(sample, CascadingEncodings);
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

    internal DataColumn CreateSample(DataColumn data)
    {
        int length = data.LogicalLength;
        if (length < SampleCount)
        {
            return data;
        }
        
        // Need to calculate sample length first, to round correctly.
        var sampleLength = (int)(length * SamplePercentage) / SampleCount;
        var totalSampleLength = sampleLength * SampleCount;
        int size = data.LogicalType.TryGetSize(out int s) ? s : 1;
        DataColumnBuilder builder = new DataColumnBuilder(data.LogicalType, totalSampleLength * size, true);
        DataColumnReader reader = data.OpenReader();
        
        int sectionLength = length / SampleCount;
        for (int i = 0; i < SampleCount; i++)
        {
            int index = Random.Shared.Next(0, sectionLength - sampleLength);
            reader.AdvanceUnits(index);
            builder.WriteRaw(reader.ReadUnits(sampleLength), sampleLength);
            reader.AdvanceUnits(sectionLength - index - sampleLength);
        }

        return builder.Build();
    }
}