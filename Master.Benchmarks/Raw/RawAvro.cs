using System.Text;
using Avro;
using Avro.File;
using Avro.Generic;

namespace Master.Benchmarks.Raw;

internal sealed class RawAvro : IRawBenchmark
{
    public void Write(string path, Data data)
    {
        Schema schema = Schema.Parse(CreateSchema(data));
        using IFileWriter<GenericRecord> writer =
            DataFileWriter<GenericRecord>.OpenWriter(new GenericDatumWriter<GenericRecord>(schema), path);

        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (IEnumerable<object> row in data.RowMajor())
            {
                GenericRecord record = new GenericRecord((RecordSchema)schema);
                foreach ((object cell, int colIndex) in row.Select((c, i) => (c, i)))
                {
                    record.Add(colIndex, cell);
                }
                writer.Append(record);
            }
        }
    }

    private string CreateSchema(Data data)
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
                type == typeof(double) ? "double" :
                throw new NotImplementedException();
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