using System.Runtime.CompilerServices;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult.Columns;

internal sealed class DictionaryColumn : IColumnParent
{
    public DictionaryColumn(LogicalType logicalType, IColumn valuesColumn, IColumn indexColumn, int logicalLength)
    {
        LogicalType = logicalType;
        ValuesColumn = valuesColumn;
        IndexColumn = indexColumn;
        LogicalLength = logicalLength;
    }

    public EncodingType EncodingType { get; } = EncodingType.Dictionary;
    public LogicalType LogicalType { get; }

    public IColumn ValuesColumn { get; set; }
    public IColumn IndexColumn { get; set; }
    public int LogicalLength { get; set; }

    public void WriteMetadata(IBlobBuilder blobBuilder)
    {
    }

    public IColumnReader OpenReader()
    {
        return DictionaryEncoding.CreateReader(LogicalType, ValuesColumn.OpenReader(), IndexColumn.OpenReader<int>(), LogicalLength);
    }

    public IEnumerable<IColumn> GetChildColumns()
    {
        yield return ValuesColumn;
        yield return IndexColumn;
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        if (existingColumn.Equals(ValuesColumn))
        {
            ValuesColumn = newColumn;
            return true;
        }
        if (existingColumn.Equals(IndexColumn))
        {
            IndexColumn = newColumn;
            return true;
        }

        return false;
    }
}
