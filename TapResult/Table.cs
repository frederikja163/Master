using System.Diagnostics;
using TapResult.Columns;
using TapResult.Encodings;

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

    public void Swap(in IColumn existingColumn, in IColumn newColumn)
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            IColumn column = _columns[i];
            if (!existingColumn.Equals(column)) 
                continue;
            _columns[i] = column;
            break;
        }
    }

    public void WriteMetadata(ref ColumnBuilder blobBuilder)
    {
        ColumnBuilder builder = new ColumnBuilder(LogicalType.String, 100, false);
        builder.WriteString(Name);
        builder.WriteStrings(_names);
        blobBuilder.WriteBlob(builder.Build().Data.ToArray());
    }

    internal Table(IEnumerable<IColumn> columns, IEnumerable<string> names, string name)
    {
        _columns = columns.ToArray();
        _names = names.ToArray();
        Name = name;
        Debug.Assert(_columns.Count() == _names.Count());
    }
}