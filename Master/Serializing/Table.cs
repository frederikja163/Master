using System.Diagnostics;
using System.Text;
using Master.Serializing.Columns;
using Master.Serializing.Encodings;

namespace Master.Serializing;

public struct Table : IColumn
{
    public IEnumerable<(IColumn column, string name)> Columns => _columns;
    private (IColumn column, string name)[] _columns;

    public int CalculateTotalLength()
    {
        return GetDataColumns().Sum(column => column.CalculateTotalLength());
    }

    public EncodingId Id => EncodingId.Table;
    public IEnumerable<DataColumn> GetDataColumns()
    {
        foreach ((IColumn column, string _) in Columns)
        {
            foreach (DataColumn dataColumn in column.GetDataColumns()) 
                yield return dataColumn;
        }
    }

    void IColumn.WriteMetadata(DataColumnBuilder builder)
    {
        throw new NotImplementedException();
    }

    public Table(IEnumerable<(DataColumn column, string name)> columns)
    {
        Serializer serializer = new();
        _columns = columns.Select(item => (serializer.Encode(item.column), item.name)).ToArray();
    }

    internal Table(DataColumn[] columns, string[] names)
    {
        Debug.Assert(columns.Length == names.Length);
        Serializer serializer = new();
        _columns = columns.Zip(names).Select(item => (serializer.Encode(item.First), item.Second)).ToArray();
    }
}