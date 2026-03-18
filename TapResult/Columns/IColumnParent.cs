namespace TapResult.Columns;

/// <summary>
/// A parent of other 
/// </summary>
public interface IColumnParent : IColumn
{
    /// <summary>
    /// Gets all child columns
    /// </summary>
    /// <param name="recursive">If true, returns depth first all children and children's children</param>
    /// <returns></returns>
    public IEnumerable<IColumn> GetChildColumns(bool recursive = false);
    public void Swap(in IColumn existingColumn, in IColumn newColumn);
}