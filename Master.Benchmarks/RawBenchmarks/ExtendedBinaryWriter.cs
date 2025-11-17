namespace Master.Benchmarks;

// For now there is no way to read this data back as there is no schema for the extended binary writer.
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
       WriteStrings(array);
       WriteValueType<int>(array);
       WriteValueType<float>(array);
    }

    private unsafe void WriteValueType<T>(Array array)
        where T : unmanaged
    {
        var type = array.GetType().GetElementType();
        if (type != typeof(T))
        {
            return;
        }
        byte[] bytes = new byte[array.Length * sizeof(T)];
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