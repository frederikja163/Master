using TapResult;
using TapResult.Columns;
using TapResult.Benchmarks.Data;
using IColumn = TapResult.Columns.IColumn;

namespace TapResult.Benchmarks.Raw;

internal abstract class TapResultBenchmarkBase : IRawBenchmark
{
    protected WriterBase? Writer;
    private Func<string, WriterBase> _writerCreator;

    public TapResultBenchmarkBase(Func<string, WriterBase> writerCreator)
    {
        _writerCreator = writerCreator;
    }
    
    public void Open(string filePath)
    {
        Writer = _writerCreator(filePath);
    }

    public abstract void Write(ICustomData data);

    public virtual void Close()
    {
        if (Writer is IDisposable disposable)
        {
            disposable?.Dispose();
        }
    }
}

internal sealed class CompressedTapResultBenchmark : TapResultBenchmarkBase
{
    public CompressedTapResultBenchmark(Func<string, WriterBase> writerCreator) : base(writerCreator)
    {
    }

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


internal sealed class AsyncTapResultBenchmark : TapResultBenchmarkBase
{
    private readonly List<Task> _tasks = new List<Task>();

    public AsyncTapResultBenchmark(Func<string, WriterBase> writerCreator) : base(writerCreator)
    {
    }

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

internal sealed class TapResultBenchmark : TapResultBenchmarkBase
{
    public TapResultBenchmark(Func<string, WriterBase> writerCreator) : base(writerCreator)
    {
    }

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

internal static class TapResultCreators
{
    public static WriterBase CreateBase(string path)
    {
        return new TapResultWriter(File.Create(path));
    }
}