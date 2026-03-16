using BenchmarkDotNet.Columns;
using Master.Benchmarks.Data;
using Master.Serializing;
using Master.Serializing.Columns;
using IColumn = Master.Serializing.Columns.IColumn;

namespace Master.Benchmarks.Raw;

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
                DataColumn dataColumn = DataColumn.Create(array, out var nulls);
                IColumn column = serializer.Encode(dataColumn);
                foreach (DataColumn col in column.GetDataColumns())
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
                    DataColumn dataColumn = DataColumn.Create(array, out var nulls);
                    IColumn column = serializer.Encode(dataColumn);
                    lock (stream)
                    {
                        foreach (DataColumn col in column.GetDataColumns())
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
                DataColumn dataColumn = DataColumn.Create(array, out var nulls);
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