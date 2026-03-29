using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawBinaryStream(string filePath) : IRawBenchmark
{
    private ExtendedBinaryWriter _writer = new ExtendedBinaryWriter(filePath);
    
    public void Write(ICustomData data)
    {
        foreach (Array array in data.Columns)
        {
            _writer.Write(array);
        }
    }

    public override string ToString()
    {
        return "Binary";
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}