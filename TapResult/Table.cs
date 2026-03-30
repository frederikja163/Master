using System.Diagnostics;
using TapResult.Columns;
using TapResult.Encodings;
using TapResult.Readers;

namespace TapResult;

public sealed class Table : IColumnParent
{
    private readonly IColumn[] _columns;
    public int ColumnCount => _columns.Length;
    internal IEnumerable<IColumn> Columns => _columns;
    private readonly string[] _names;
    public IEnumerable<string> Names => _names;
    private string Name { get; }


    public EncodingType EncodingType => EncodingType.Table;
    public LogicalType LogicalType => LogicalType.UInt8;

    public IEnumerable<IColumn> GetChildColumns()
    {
        foreach (IColumn column in _columns)
        {
            yield return column;
        }
    }

    public bool Swap(IColumn existingColumn, IColumn newColumn)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            IColumn column = _columns[i];
            if (!existingColumn.Equals(column)) 
                continue;
            _columns[i] = newColumn;
            return true;
        }

        return false;
    }

    public void WriteMetadata(ColumnBuilder blobBuilder)
    {
        ColumnBuilder builder = new ColumnBuilder(LogicalType.String, 100);
        builder.WriteString(Name);
        builder.WriteStrings(_names);
        blobBuilder.WriteBlob(builder.BuildDataColumn().Data.ToArray());
    }

    IColumnReader IColumn.OpenReader()
    {
        throw new Exception("Cannot open a reader for tables.");
    }

    public void Compress(Encoder? encoder = null)
    {
        encoder ??= Encoder.Default;
        foreach (DataColumn column in this.GetChildColumnsRecursive().OfType<DataColumn>())
        {
            IColumn encodedColumn = encoder.Encode(column);
            this.SwapRecursive(column, encodedColumn);
        }
    }

    public Table(IEnumerable<IColumn> columns, IEnumerable<string> names, string name)
    {
        _columns = columns.ToArray();
        _names = names.ToArray();
        Name = name;
        Debug.Assert(_columns.Count() == _names.Count());
    }
}