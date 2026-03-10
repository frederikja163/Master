using System.Diagnostics;
using System.Text;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Serializing;

public struct Table : IColumnParent
{
    private readonly IColumn[] _columns;
    public int ColumnCount => _columns.Length;
    internal IEnumerable<IColumn> Columns => _columns;
    private readonly string[] _names;
    public IEnumerable<string> Names => _names;
    private readonly string Name { get; }


    public EncodingId EncodingId => EncodingId.Table;
    public LogicalType LogicalType => LogicalType.UInt8;
    IEnumerable<IColumn> IColumnParent.GetChildColumns(bool recursive)
    {
        foreach (IColumn column in _columns)
        {
            if (recursive && column is IColumnParent columnParent)
            {
                foreach (IColumn childColumn in columnParent.GetChildColumns(recursive))
                {
                    yield return childColumn;
                }
            }
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
    public int CalculateTotalLength()
    {
        return GetDataColumns().Sum(column => column.CalculateTotalLength());
    }

    public IEnumerable<DataColumn> GetDataColumns()
    {
        foreach (IColumn column in _columns)
        {
            foreach (DataColumn dataColumn in column.GetDataColumns()) 
                yield return dataColumn;
        }
    }

    void IColumn.WriteMetadata(ref DataColumnBuilder blobBuilder)
    {
        DataColumnBuilder builder = new DataColumnBuilder(LogicalType.String, 100, false);
        builder.WriteString(Name);
        builder.WriteStrings(_names);
        blobBuilder.WriteBlob(builder.Build().Data.ToArray());
    }

    internal Table(IEnumerable<DataColumn> columns, IEnumerable<string> names, string name)
    {
        _columns = columns.OfType<IColumn>().ToArray();
        _names = names.ToArray();
        Name = name;
        Debug.Assert(_columns.Count() == _names.Count());
    }
}