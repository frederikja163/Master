using Parquet;
using Parquet.Data;
using Parquet.Schema;

namespace Master.Benchmarks.RawBenchmarks;

internal sealed class RawParquet : IRawBenchmark
{
    public Data Data { get; set; }

    public void Write()
    {
        Task.Run(async () =>
        {
            ParquetSchema schema = new(
                Data.ColumnNames.Zip(Data.Columns)
                    .Select(tuple => new DataField(tuple.First, tuple.Second.GetType().GetElementType()!))
            );

            await using Stream stream = File.OpenWrite(Config.FilePath);

            await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream);

            using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
            foreach ((DataField field, Array data) in schema.Fields.Cast<DataField>().Zip(Data.Columns))
            {
                await groupWriter.WriteColumnAsync(new DataColumn(field, data));
            }
        }).Wait();
    }

    public override string ToString()
    {
        return "Parquet";
    }
}