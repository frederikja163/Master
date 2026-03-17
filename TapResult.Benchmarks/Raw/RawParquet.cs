using TapResult.Extensions;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawParquet(CompressionMethod method) : IRawBenchmark
{
    public void Write(string filePath, ICustomData data)
    {
        Task.Run(async () =>
        {
            ParquetSchema schema = new(
                data.ColumnNames.Zip(data.Columns)
                    .Select(tuple => new DataField(tuple.First, GetType(tuple.Second), IsNullable(tuple.Second)))
            );

            await using Stream stream = File.OpenWrite(filePath);

            await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream);
            writer.CompressionMethod = method;

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < data.Repeats; i++)
            {
                tasks.Add(Task.Run(async () =>
                {

                    using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
                    foreach ((DataField field, Array values) in schema.Fields.Cast<DataField>().Zip(data.Columns))
                    {
                        await groupWriter.WriteColumnAsync(new DataColumn(field, values));
                    }
                }));
            }

            Task.WaitAll(tasks);
        }).Wait();
    }

    private static Type GetType(Array arr)
    {
        return arr.GetType().GetElementType()!.GetUnderlyingNullableType();
    }

    public static bool IsNullable(Array arr)
    {
        return arr.GetType().GetElementType()!.IsNullable();
    }

    public override string ToString()
    {
        return $"Parquet (Compression: {method})";
    }
}