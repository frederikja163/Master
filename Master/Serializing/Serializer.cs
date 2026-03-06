using System.Diagnostics;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Serializing;

public sealed class Serializer
{
    private readonly ILookup<LogicalType, IEncoding> _encodingsByType;
    private readonly Dictionary<EncodingId, IEncoding> _encodingsById;
    private DataColumnBuilder _idBuilder = new (LogicalType.SInt32, 50, false);
    private DataColumnBuilder _parentIdBuilder = new (LogicalType.SInt32, 50, false);
    private DataColumnBuilder _encodingIdBuilder = new (LogicalType.UInt8, 50, false);
    private DataColumnBuilder _logicalTypeBuilder = new(LogicalType.UInt8, 50, false);
    private DataColumnBuilder _blobBuilder = new (LogicalType.Blob, 50, false);
    private int _currentId = 0;

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
    
    public IColumn Encode(in DataColumn column)
    {
        DataColumn sample = CreateSample(column);
        IColumn metadataSample = PickEncoding(sample, CascadingEncodings);
        return Encode(column, metadataSample);
    }
    public void Encode(ref Table table)
    {
        foreach (DataColumn dataColumn in table.GetDataColumns())
        {
            table.Swap(dataColumn, Encode(dataColumn));
        }
    }

    private IColumn Encode(DataColumn inData, IColumn metadataSample)
    {
        if (metadataSample is DataColumn col)
        {
            return inData;
        }
        EncodingId id = metadataSample.EncodingId;
        
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
                foreach (var child in parent.GetDataColumns())
                {
                    IColumn column = child;
                    parent.Swap(column, PickEncoding(child, cascades - 1));
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

    internal DataColumn CreateSample(in DataColumn data)
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
        GenericReader reader = data.OpenGenericReader();
        
        int sectionLength = length / SampleCount;
        for (int i = 0; i < SampleCount; i++)
        {
            int index = Random.Shared.Next(0, sectionLength - sampleLength);
            reader.AdvanceUnits(data.LogicalType, index);
            builder.WriteRaw(reader.ReadUnits(data.LogicalType, sampleLength), sampleLength);
            reader.AdvanceUnits(data.LogicalType, sectionLength - index - sampleLength);
        }

        return builder.Build();
    }
    internal void WriteMetadata(Table table)
    {
        WriteMetaDataForColumn(table, -1);
    }
    internal void WriteMetaDataForColumn(IColumn column, int parentId)
    {
        int id = _currentId++;
        if (column is IColumnParent parent)
        {
            foreach (IColumn childColumn in parent.GetChildColumns()) 
                WriteMetaDataForColumn(childColumn, id);
        }
        _idBuilder.Write(id);
        _parentIdBuilder.Write(parentId);
        _encodingIdBuilder.Write((byte) column.EncodingId);
        _logicalTypeBuilder.Write((byte) column.LogicalType);
        column.WriteMetadata(ref _blobBuilder);
    }

    internal Table GetMetadata()
    {
        return new Table([_idBuilder.Build(), _parentIdBuilder.Build(), _encodingIdBuilder.Build(), _logicalTypeBuilder.Build(), _blobBuilder.Build()], ["Id", "ParentId", "Encoding", "LogicalType", "Blob"], "metadata");
    }
}