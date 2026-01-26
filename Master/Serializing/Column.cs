namespace Master.Serializing;

internal sealed class Column
{
    public required ReadOnlyMemory<byte> Parameters { get; init; }
    public required PhysicalColumn[] PhysicalColumns { get; init; }
}