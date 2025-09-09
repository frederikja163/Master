namespace Master.Benchmarks;

internal sealed class ExtendedBinaryWriter : IDisposable
{
    private readonly Stream _stream;
    private readonly BinaryWriter _writer;
    
    public ExtendedBinaryWriter(string filePath)
    {
        _stream = File.OpenWrite(filePath);
        _writer = new BinaryWriter(_stream);
    }

    public void Write(Array array)
    {
       WriteInts(array);
       WriteStrings(array);
    }

    private void WriteInts(Array array)
    {
        if (array.GetType().GetElementType() != typeof(int))
        {
            return;
        }
        byte[] bytes = new byte[array.Length * sizeof(int)];
        Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
        _writer.Write(bytes);
    }

    private void WriteStrings(Array array)
    {
        if (array.GetType().GetElementType() != typeof(string))
        {
            return;
        }
        foreach (string str in array.Cast<string>())
        {
            _writer.Write(str);
        }
    }
    
    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }
}