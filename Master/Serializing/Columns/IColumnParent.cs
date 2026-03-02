namespace Master.Serializing.Columns;

public interface IColumnParent : IColumn
{
    internal IEnumerable<IColumn> GetChildColumns();
    internal void Swap(ref IColumn existingColumn, IColumn newColumn);
}