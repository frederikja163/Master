using Master.Serializing;

namespace Master.Benchmarks.Raw;

internal sealed class EncodingBenchmark : IRawBenchmark
{
    public void Write(string path, Data data)
    {
        Serializer serializer = new Serializer();
        Stream stream = File.OpenWrite(path);
        BinaryWriter writer = new BinaryWriter(stream);

        foreach (Array array in data.Columns)
        {
            DataColumn dataColumn = DataColumn.Create(array);
            MetadataColumn column = serializer.Encode(dataColumn);
            foreach (DataColumn col in column.GetDataColumns())
            {
                writer.Write(col.Data.Span);
            }
        }
    }
}