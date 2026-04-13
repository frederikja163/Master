using TapResult.Benchmarks.Data;

namespace TapResult.Benchmarks.Raw;

internal sealed class RawCsv : IRawBenchmark, IAsyncDisposable
{
    private Stream? _stream;
    private StreamWriter? _writer;

    public void Open(string filePath)
    {
        _stream = File.Create(filePath);
        _writer = new StreamWriter(_stream);
    }

    public void Write(ICustomData data)
    {
        _writer!.WriteLine(string.Join(",", data.ColumnNames));
        
        foreach (Array row in data.Rows)
        {
            _writer.WriteLine(string.Join(",", row.OfType<object>().Select(o => o.ToString() ?? "")));
        }
    }

    public override string ToString()
    {
        return "CSV";
    }

    public void Close()
    {
        _stream!.Dispose();
        _writer!.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream!.DisposeAsync();
        await _writer!.DisposeAsync();
    }
}