using Dia2Lib;
using TapResult.Extensions;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawParquet : IRawBenchmark
{
    private string _basePath;
    private readonly CompressionMethod _compressionMethod;
    private List<string> _paths = new List<string>();
    private Dictionary<string, List<DataField>> _fields = new Dictionary<string, List<DataField>>();

    public RawParquet(CompressionMethod compressionMethod)
    {
        _compressionMethod = compressionMethod;
    }

    public void Open(string filePath)
    {
        _basePath = filePath;
    }

    public void Write(ICustomData data)
    {
        Task.Run(async () =>
        {
            List<DataField> fields = new List<DataField>();
            foreach ((var name, Array values) in data.ColumnNames.Zip(data.Columns))
            {
                DataField? field = null;
                Type parquetType = GetParquetType(values);
                if (_fields.TryGetValue(name, out var existingFields))
                {
                    field = existingFields.FirstOrDefault(d => d.ClrType == parquetType);
                }
                else
                {
                    existingFields = new List<DataField>();
                    _fields[name] = existingFields;
                }

                if (field is null)
                {
                    field = new DataField(name, parquetType, true);
                }
                
                existingFields.Add(field);
                fields.Add(field);
            }

            ParquetSchema schema = new(fields);

            await using Stream stream = File.Create(GetPath());

            await using ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream);
            writer.CompressionMethod = _compressionMethod;

            using (ParquetRowGroupWriter groupWriter = writer.CreateRowGroup())
            {
                foreach ((DataField field, Array values) in schema.Fields.Cast<DataField>().Zip(data.Columns))
                {
                    Array finalValues = values;

                    if (!IsNullable(values))
                    {
                        finalValues = ToNullableArray(values);
                    }

                    await groupWriter.WriteColumnAsync(new DataColumn(field, finalValues));
                }
            }
            stream.Flush();
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
        var elementType = source.GetType().GetElementType()!;
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

    public void Close()
    {
        Task.Run(async () =>
        {
            List<DataField> fields = _fields.Values.SelectMany(f => f).ToList();

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