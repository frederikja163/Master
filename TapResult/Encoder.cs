using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult;

/// <summary>
/// Responsible for encodings, and settings related to this.
/// </summary>
public sealed class Encoder
{
    /// <summary>
    /// Gets the default encodings, in case you want to switch any out or add your own on top of default encodings.
    /// </summary>
    public static IEnumerable<IEncoding> GetDefaultEncodings()
    {
        yield return new SplitEncoding();
        yield return new BitPacking();
    }
    
    /// <summary>
    /// Gets an encoder with all the settings as the default.
    /// </summary>
    public static Encoder Default { get; } = new Encoder();
    
    private readonly ILookup<LogicalType, IEncoding> _encodingsByType;
    private readonly Dictionary<EncodingType, IEncoding> _encodingsById;
    public IReadOnlyDictionary<EncodingType, IEncoding> EncodingsById => _encodingsById;
    
    /// <summary>
    /// Creates a new default encoder.
    /// </summary>
    public Encoder():
        this(GetDefaultEncodings())
    {
        
    }
    
    /// <summary>
    /// Creates a new encoder with custom encodings.
    /// Use <see cref="GetDefaultEncodings"/> to get a list of default encodings that can be used as a base.
    /// </summary>
    public Encoder(params IEnumerable<IEncoding> encodings)
    {
        _encodingsById = encodings.ToDictionary(e => e.Type, e => e);
        _encodingsByType = _encodingsById.Values
            .SelectMany(e => e.GetSupportedTypes().Select(t => (t, e)))
            .ToLookup(t => t.t, t => t.e);
    }

    internal IColumnReader Decode(EncodingType encodingType, LogicalType type, ref GenericReader blobReader, IEnumerable<IColumnReader> childColumns)
    {
        IEncoding encoding = _encodingsById[encodingType];
        return encoding.CreateDecoder(type, blobReader, childColumns);
    }
    
    /// <summary>
    /// Number of recursive cascades.
    /// </summary>
    /// <remarks> Only relevant on encode. </remarks>
    public int CascadingEncodings { get; init; } = 2;
    /// <summary>
    /// The percentage of columns to sample, to find the best encoding scheme.
    /// </summary>
    /// <remarks> Only relevant on encode. </remarks>
    public double SamplePercentage { get; init; } = 0.1;
    /// <summary>
    /// The total amount of separate samples to take.
    /// </summary>
    /// <remarks> Only relevant on encode. </remarks>
    public int SampleCount { get; init; } = 10;
    /// <summary>
    /// The max length of all samples combined.
    /// </summary>
    /// <remarks> Only relevant on encode. </remarks>
    public int SampleMaxLength { get; init; } = 1024;
    
    internal IColumn Encode(DataColumn column)
    {
        DataColumn sample = CreateSample(column);
        IColumn metadataSample = PickEncoding(sample, CascadingEncodings);
        return Encode(column, metadataSample);
    }

    private IColumn Encode(DataColumn inData, IColumn metadataSample)
    {
        if (metadataSample is DataColumn)
        {
            return inData;
        }
        EncodingType type = metadataSample.EncodingType;
        
        IEncoding encoding = _encodingsById[type];
        IColumn columns = encoding.Encode(inData);
        if (columns is not IColumnParent parent || metadataSample is not IColumnParent parentMeta)
            return columns;
        
        foreach ((IColumn First, IColumn Second) child in parent.GetChildColumns().Zip(parentMeta.GetChildColumns()))
        {
            if (child.First is DataColumn dataColumn)
            {
                parent.Swap(dataColumn, Encode(dataColumn, child.Second));
            }
        }
        return columns;
    }

    private IColumn PickEncoding(DataColumn sample, int cascades)
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
                foreach (DataColumn child in parent.GetChildColumns().OfType<DataColumn>())
                {
                    parent.Swap(child, PickEncoding(child, cascades - 1));
                }
            }
            int length = CalculateTotalLength(encodedColumn);
            if (length >= minSize)
                continue;
            bestEncoding = encodedColumn;
            minSize = length;
        }
        
        return bestEncoding;
    }

    private int CalculateTotalLength(IColumn column)
    {
        if (column is DataColumn dataColumn)
        {
            return dataColumn.PhysicalSize;
        }

        if (column is IColumnParent parent)
        {
            return parent.GetChildColumns().Select(CalculateTotalLength).Sum();
        }

        return 0;
    }

    internal DataColumn CreateSample(in DataColumn data)
    {
        int length = data.LogicalLength;
        if (length * SamplePercentage < SampleCount)
        {
            return data;
        }
        
        // Need to calculate sample length first, to round correctly.
        var sampleLength = (int)(length * SamplePercentage) / SampleCount;
        sampleLength = Math.Min(sampleLength, SampleMaxLength);
        var totalSampleLength = sampleLength * SampleCount;
        int size = data.LogicalType.TryGetSize(out int s) ? s : 1;
        ColumnBuilder builder = new ColumnBuilder(data.LogicalType, totalSampleLength * size);
        GenericReader reader = data.OpenGenericReader();
        
        int sectionLength = length / SampleCount;
        for (int i = 0; i < SampleCount; i++)
        {
            int index = Random.Shared.Next(0, sectionLength - sampleLength);
            reader.AdvanceUnits(data.LogicalType, index);
            builder.WriteRaw(reader.ReadUnits(data.LogicalType, sampleLength), sampleLength);
            reader.AdvanceUnits(data.LogicalType, sectionLength - index - sampleLength);
        }

        return builder.BuildDataColumn();
    }
}