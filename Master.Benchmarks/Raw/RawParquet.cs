using System.IO.Compression;
using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Master.Benchmarks.Raw;

internal sealed class RawParquet(CompressionMethod method) : IRawBenchmark
{
    public void Write(string filePath, Data data)
    {
        Task.Run(async () =>
        {
            ParquetSchema schema = new(
                data.ColumnNames.Zip(data.Columns)
                    .Select(tuple => new DataField(tuple.First, GetType(tuple), true))
            );

            await using Stream stream = File.OpenWrite(filePath);

            await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream);
            writer.CompressionMethod = method;

            for (int i = 0; i < data.Repeats; i++)
            {
                using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
                foreach ((DataField field, Array values) in schema.Fields.Cast<DataField>().Zip(data.Columns))
                {
                    await groupWriter.WriteColumnAsync(new DataColumn(field, values));
                }
            }
        }).Wait();
    }

    private static Type GetType((string First, Array Second) tuple)
    {
        return Nullable.GetUnderlyingType(tuple.Second.GetType().GetElementType()!)!;
    }

    public override string ToString()
    {
        return $"Parquet (Compression: {method})";
    }
}