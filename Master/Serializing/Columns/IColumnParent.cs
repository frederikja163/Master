namespace Master.Serializing.Columns;

public interface IColumnParent : IColumn
{
    public IColumn[] Columns { get; set; }
}