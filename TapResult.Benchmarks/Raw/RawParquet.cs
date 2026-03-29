using TapResult.Extensions;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawParquet : IRawBenchmark
{
    private readonly string _basePath;
    private readonly CompressionMethod _compressionMethod;
    private List<string> _paths = new List<string>();

    public RawParquet(string path, CompressionMethod compressionMethod)
    {
        _basePath = path;
        _compressionMethod = compressionMethod;
    }
    
    public void Write(ICustomData data)
    {
        Task.Run(async () =>
        {
            ParquetSchema schema = new(
                data.ColumnNames.Zip(data.Columns)
                    .Select(tuple => new DataField(tuple.First, GetParquetType(tuple.Second), true))
            );

            await using Stream stream = File.OpenWrite(GetPath());

            await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream);
            writer.CompressionMethod = _compressionMethod;

            using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
            foreach ((DataField field, Array values) in schema.Fields.Cast<DataField>().Zip(data.Columns))
            {
                Array finalValues = values;

                if (!IsNullable(values))
                {
                    finalValues = ToNullableArray(values);
                }

                await groupWriter.WriteColumnAsync(new DataColumn(field, finalValues));
            }

        }).GetAwaiter().GetResult();
    }

    private static Type GetParquetType(Array arr)
    {
        return arr.GetType().GetElementType()!.GetUnderlyingNullableType();
    }

    public static bool IsNullable(Array arr)
    {
        return arr.GetType().GetElementType()!.IsNullable();
    }
    
    public static Array ToNullableArray(Array source)
    {
        var elementType = source.GetType().GetElementType();
        if (!elementType.IsValueType || Nullable.GetUnderlyingType(elementType) != null)
        {
            return source;
        }

        // Create nullable type (e.g., int -> int?)
        var nullableType = typeof(Nullable<>).MakeGenericType(elementType);

        var target = Array.CreateInstance(nullableType, source.Length);

        for (int i = 0; i < source.Length; i++)
        {
            var value = source.GetValue(i);
            target.SetValue(value, i); // boxing handles conversion
        }

        return target;
    }

    public override string ToString()
    {
        return $"Parquet (Compression: {_compressionMethod})";
    }

    public string GetPath()
    {
        string path = _basePath + _paths.Count;
        _paths.Add(path);
        return path;
    }

    public void Dispose()
    {
        Task.Run(async () =>
        {
            List<DataField> fields = new List<DataField>();
            foreach (string path in _paths)
            {
                ParquetSchema schema = await ParquetReader.ReadSchemaAsync(path);
                foreach (DataField field in schema.DataFields)
                {
                    if (fields.Any(f => f.ClrType == field.ClrType && f.Name == field.Name))
                    {
                        continue;
                    }

                    if (fields.Any(f => f.Name == field.Name))
                    {
                        fields.Add(new DataField(field.Name + field.ClrType.Name, field.ClrType, field.IsNullable));
                    }
                    else
                    {
                        fields.Add(field);
                    }
                }
            }

            ParquetSchema writeSchema = new ParquetSchema(fields);
            using Stream stream = File.Create(_basePath);
            using ParquetWriter writer = await ParquetWriter.CreateAsync(writeSchema, stream);
            writer.CompressionMethod = _compressionMethod;

            foreach (string path in _paths)
            {
                using ParquetRowGroupWriter groupWriter = writer.CreateRowGroup();
                
                using Stream readStream = File.OpenRead(path);
                using ParquetReader reader = await ParquetReader.CreateAsync(readStream);
                using ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(0);
                foreach (DataField field in fields)
                {
                    if (groupReader.ColumnExists(field))
                    {
                        DataColumn column = await groupReader.ReadColumnAsync(field);
                        await groupWriter.WriteColumnAsync(column);
                    }
                    else
                    {
                        Array array = Array.CreateInstance(field.ClrNullableIfHasNullsType, groupReader.RowCount);
                        await groupWriter.WriteColumnAsync(new DataColumn(field, array));
                    }
                }
            }
        }).GetAwaiter().GetResult();
    }
}