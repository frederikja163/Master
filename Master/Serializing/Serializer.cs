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
    
    public EncodedColumn Encode(ReadOnlySpan<string> data)
    {
        Debug.Assert(data.Length > 0);
        
        ReadOnlySpan<string> sample = CreateSample<string>(data);
        EncodedColumn encodedSample = PickEncoding(PhysicalColumn.Create(sample), CascadingEncodings);
        PhysicalColumn column = PhysicalColumn.Create(data);
        return Encode(column, encodedSample);
    }
    
    public EncodedColumn Encode<T>(ReadOnlySpan<T> data) where T : struct
    {
        ReadOnlySpan<T> sample = CreateSample<T>(data);
        EncodedColumn encodedSample = PickEncoding(PhysicalColumn.Create(sample), CascadingEncodings);
        PhysicalColumn column = PhysicalColumn.Create(data);
        return Encode(column, encodedSample);
    }

    public PhysicalColumn Decode(EncodedColumn column)
    {
        if (column.Id == EncodingId.Binary)
        {
            return PhysicalColumn.Create(column.Parameters.Span);
        }
        
        ReadOnlyMemory<byte>[] physicalColumns = column.Columns.Select(c => Decode(c).Data).ToArray();
        return _encodingsById[column.Id].Decode(physicalColumns, column.Parameters);
    }

    private EncodedColumn Encode(PhysicalColumn inData, EncodedColumn encodedSample)
    {
        EncodingId id = encodedSample.Id;

        if (id == EncodingId.Binary)
        {
            return new EncodedColumn(inData.Data);
        }
        
        IEncoding encoding = _encodingsById[id];
        Column outColumn = encoding.Encode(inData, encodedSample.Parameters);
        EncodedColumn[] column = outColumn
            .PhysicalColumns
            .Zip(encodedSample.Columns)
            .Select(t => Encode(t.First, t.Second))
            .ToArray();
        ReadOnlyMemory<byte> parameters = outColumn.Parameters;
        return new EncodedColumn(id, parameters, column);
    }

    internal EncodedColumn PickEncoding(PhysicalColumn sample, int cascades)
    {
        if (cascades == 0)
        {
            return new EncodedColumn(sample.Data);
        }
        int minSize = sample.PhysicalSize;
        EncodedColumn bestEncoding = new EncodedColumn(sample.Data);
        foreach (IEncoding encoding in _encodingsByType[sample.LogicalType])
        {
            Column output = encoding.Encode(sample);
            EncodedColumn[] childColumns = output
                .PhysicalColumns
                .Select(d => PickEncoding(d, cascades - 1))
                .ToArray();
            ReadOnlyMemory<byte> parameters = output.Parameters;
            EncodedColumn encodedColumn = new EncodedColumn(encoding.Id, parameters, childColumns);
            int length = encodedColumn.CalculateTotalLength();
            if (length < minSize)
            {
                bestEncoding = encodedColumn;
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