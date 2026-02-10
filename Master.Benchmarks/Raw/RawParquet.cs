using Master.Benchmarks.Data;
using Master.Benchmarks.Extensions;
using Master.Benchmarks.Raw.Visitors;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using SqlParser;
using SqlParser.Ast;

namespace Master.Benchmarks.Raw;

internal sealed class RawParquet(CompressionMethod method) : IRawBenchmark
{
    public void Write(string filePath, ICustomData data)
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

    public void Read(string path, Sequence<Statement> sql)
    {
        Task.Run(async () =>
        {
            using ParquetReader reader = await ParquetReader.CreateAsync(path);
            foreach (IParquetRowGroupReader parquetRowGroupReader in reader.RowGroups)
            {
                sql.Visit(new ParquetWhereVisitor());
                //parquetRowGroupReader.GetStatistics()
            }
            
        }).Wait();
    }

    private static Type GetType((string First, Array Second) tuple)
    {
        return tuple.Second.GetType().GetElementType()!.GetUnderlyingNullableType();
    }

    public override string ToString()
    {
        return $"Parquet (Compression: {method})";
    }
}