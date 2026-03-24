namespace TapResult.Columns;

/// <summary>
/// A parent of other columns.
/// </summary>
public interface IColumnParent : IColumn
{
    /// <summary>
    /// Gets all immediate child columns of this parent.
    /// </summary>
    public IEnumerable<IColumn> GetChildColumns();
    /// <summary>
    /// Swaps two columns, returns true if any columns where swapped, false otherwise.
    /// </summary>
    public bool Swap(IColumn existingColumn, IColumn newColumn);
}

/// <summary>
/// Helper methods for <see cref="IColumnParent"/>.
/// </summary>
public static class ColumnParentExtensions
{
    /// <summary>
    /// Gets all child columns recursively.
    /// That means you get the children, grandchildren etc.
    /// </summary>
    public static IEnumerable<IColumn> GetChildColumnsRecursive(this IColumnParent columnParent)
    {
        foreach (IColumn column in columnParent.GetChildColumns())
        {
            if (column is IColumnParent parent)
            {
                foreach (IColumn child in GetChildColumnsRecursive(parent))
                {
                    yield return child;
                }
            }

            yield return column;
        }
    }

    public static bool SwapRecursive(this IColumnParent columnParent, IColumn existingColumn, IColumn newColumn)
    {
        if (columnParent.Swap(existingColumn, newColumn))
            return true;
        
        foreach (IColumnParent column in columnParent.GetChildColumns().OfType<IColumnParent>())
        {
            if (column.SwapRecursive(existingColumn, newColumn))
                return true;
        }

        return false;
    }
}