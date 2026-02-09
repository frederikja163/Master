using Avro;
using Avro.File;
using Avro.Generic;
using Master.Benchmarks.Data;

namespace Master.Benchmarks.Raw;

internal sealed class RawAvro : IRawBenchmark
{
    public void Write(string path, ICustomData data)
    {
        Schema schema = Schema.Parse(CreateSchema(data));
        using IFileWriter<GenericRecord> writer =
            DataFileWriter<GenericRecord>.OpenWriter(new GenericDatumWriter<GenericRecord>(schema), path);

        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array row in data.Rows)
            {
                GenericRecord record = new GenericRecord((RecordSchema)schema);
                for (int j = 0; j < row.Length; j++)
                {
                    record.Add(j, row.GetValue(j));
                }
                writer.Append(record);
            }
        }
    }

    private string CreateSchema(ICustomData data)
    {
        return $"{{{string.Join(
            ",",
            @"""type"":""record""",
            @"""name"":""data""",
            $@"""fields"":[{string.Join(",",
                data.ColumnNames.Zip(data.Columns.Select(GetArrayType)).Select(CreateField)
                )}]"
        )}}}";
        
        static string GetArrayType(Array array)
        {
            Type type = array.GetType().GetElementType() ?? throw new ArgumentException(null, nameof(array));
            return type == typeof(int) ? "int" :
                type == typeof(string) ? "string" :
                type == typeof(float) ? "float" :
                throw new NotImplementedException(type.ToString());
        }

        static string CreateField((string name, string type) tuple)
        {
            (string name, string type) = tuple;
            return $"{{{string.Join(",",
                $@"""name"":""{name}""",
                $@"""type"":""{type}"""
                )}}}";
        }
    }

    public override string ToString()
    {
        return "Avro";
    }
}