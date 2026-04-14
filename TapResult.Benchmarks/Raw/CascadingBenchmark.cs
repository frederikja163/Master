using TapResult;
using TapResult.Columns;
using TapResult.Benchmarks.Data;
using IColumn = TapResult.Columns.IColumn;

namespace TapResult.Benchmarks.Raw;

internal abstract class TapResultBenchmark : IRawBenchmark
{
    protected Writer? Writer;

    public void Open(string filePath)
    {
        Writer = new Writer(File.Create(filePath));
    }

    public abstract void Write(ICustomData data);

    public virtual void Close()
    {
        Writer?.Dispose();
    }
}

internal sealed class CascadingBenchmark : TapResultBenchmark
{
    
    public override void Write(ICustomData data)
    {
        Table table = new Table(data.Columns.Select(ColumnBuilder.Create), data.ColumnNames, data.Name);
        table.Compress();
        Writer?.Write(table);
    }

    public override string ToString()
    {
        return "TapResult Compressed";
    }
}


internal sealed class CascadingAsyncBenchmark : TapResultBenchmark
{
    private readonly List<Task> _tasks = new List<Task>();
    
    public override void Write(ICustomData data)
    {
        _tasks.Add(Task.Run(async () =>
        {
            Table table = new Table(data.Columns.Select(ColumnBuilder.Create), data.ColumnNames, data.Name);
            await table.CompressAsync();
            lock (Writer!)
            {
                Writer.Write(table);
            }
        }));
    }

    public override void Close()
    {
        Task.WaitAll(_tasks);
        base.Close();
    }

    public override string ToString()
    {
        return "TapResult Async";
    }
}

internal sealed class EncodingBenchmark : TapResultBenchmark
{
    public override void Write(ICustomData data)
    {
        Table table = new Table(data.Columns.Select(ColumnBuilder.Create), data.ColumnNames, data.Name);
        Writer!.Write(table);
    }

    public override string ToString()
    {
        return "TapResult";
    }
}