namespace Master.Columns;

public interface IColumnParent : IColumn
{
    /// <summary>
    /// Gets all child columns
    /// </summary>
    /// <param name="recursive">If true, returns depth first all children and children's children</param>
    /// <returns></returns>
    internal IEnumerable<IColumn> GetChildColumns(bool recursive = false);
    internal void Swap(in IColumn existingColumn, in IColumn newColumn);
}