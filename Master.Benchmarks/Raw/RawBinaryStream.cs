using Master.Benchmarks.Data;
using SqlParser;
using SqlParser.Ast;

namespace Master.Benchmarks.Raw;

internal sealed class RawBinaryStream : IRawBenchmark
{
    public void Write(string filePath, ICustomData data)
    {
        using ExtendedBinaryWriter writer = new ExtendedBinaryWriter(filePath);
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array array in data.Columns)
            {
                writer.Write(array);
            }
        }
    }

    public void Read(string path, Statement sql)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Binary";
    }
}