using TapResult;
using TapResult.Columns;
using TapResult.Benchmarks.Data;
using IColumn = TapResult.Columns.IColumn;

namespace TapResult.Benchmarks.Raw;

internal sealed class CascadingBenchmark : IRawBenchmark
{
    public void Write(string path, ICustomData data)
    {
        Serializer serializer = new Serializer();
        using Stream stream = File.OpenWrite(path);
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array array in data.Columns)
            {
                DataColumn dataColumn = ColumnBuilder.Create(array, out var nulls);
                IColumn column = serializer.Encode(dataColumn);
                if (column is not IColumnParent parent)
                    return;
                foreach (DataColumn col in parent.GetChildColumnsRecursive().OfType<DataColumn>())
                {
                    stream.Write(col.Data.Span);
                    if (nulls is not null)
                    {
                        stream.Write(nulls.Data.Span);
                    }
                }

            }
        }
    }

    public override string ToString()
    {
        return "Cascading";
    }
}


internal sealed class CascadingAsyncBenchmark : IRawBenchmark
{
    public void Write(string path, ICustomData data)
    {
        Serializer serializer = new Serializer();
        using Stream stream = File.OpenWrite(path);
        List<Task> tasks = new ();
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array array in data.Columns)
            {
                tasks.Add(Task.Run(() =>
                {
                    DataColumn dataColumn = ColumnBuilder.Create(array, out var nulls);
                    IColumn column = serializer.Encode(dataColumn);
                    if (column is not IColumnParent parent)
                        return;
                    lock (stream)
                    {
                        foreach (DataColumn col in parent.GetChildColumnsRecursive().OfType<DataColumn>())
                        {
                            stream.Write(col.Data.Span);
                            if (nulls is not null)
                            {
                                stream.Write(nulls.Data.Span);
                            }
                        }
                    }
                }));
            }
        }

        Task.WaitAll(tasks);
    }

    public override string ToString()
    {
        return "Async Cascading";
    }
}

internal sealed class EncodingBenchmark : IRawBenchmark
{
    public void Write(string path, ICustomData data)
    {
        Stream stream = File.OpenWrite(path);

        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array array in data.Columns)
            {
                DataColumn dataColumn = ColumnBuilder.Create(array, out var nulls);
                stream.Write(dataColumn.Data.Span);
                if (nulls is not null)
                {
                    stream.Write(nulls.Data.Span);
                }
            }
        }
    }

    public override string ToString()
    {
        return "Encoding";
    }
}