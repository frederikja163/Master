using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class DeltaColumn : IColumnParent
{
    public DeltaColumn(LogicalType logicalType, IColumn deltas, int logicalLength, byte[] firstValueBytes)
    {
        LogicalType = logicalType;
        Deltas = deltas;
        LogicalLength = logicalLength;
        _firstValueBytes = firstValueBytes;
    }

    public EncodingType EncodingType { get; } = EncodingType.Delta;
    public LogicalType LogicalType { get; }
    public IColumn Deltas { get; set; }
    public int LogicalLength { get; set; }

    private readonly byte[] _firstValueBytes;

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
        blobBuilder.WriteRaw(_firstValueBytes);
    }

    public IColumnReader OpenReader()
    {
        GenericReader metadataReader = new(new ReadOnlyMemory<byte>(_firstValueBytes));
        return DeltaEncoding.CreateReader(LogicalType, Deltas.OpenReader(), LogicalLength, metadataReader);
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return Deltas;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn.Equals(Deltas))
        {
            Deltas = newColumn;
            return true;
        }
        return false;
    }
}
