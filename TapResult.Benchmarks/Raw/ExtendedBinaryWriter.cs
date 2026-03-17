using System.Runtime.InteropServices;

namespace TapResult.Benchmarks.Raw;

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
        if (WriteStrings(array)) return;
        if (TryWriteValueType<int>(array)) return;
        if (TryWriteValueType<float>(array)) return;
        if (TryWriteValueType<double>(array)) return;
        if (WriteNullableType<int>(array)) return;
        if (WriteNullableType<float>(array)) return;
        if (WriteNullableType<double>(array)) return;
    }

    private unsafe bool TryWriteValueType<T>(Array array)
        where T : unmanaged
    {
        var type = array.GetType().GetElementType()!;
        if (type != typeof(T))
        {
            return false;
        }
        WriteValueType<T>((T[]) array);
        return true;
    }

    private unsafe void WriteValueType<T>(Span<T> span)
        where T : unmanaged
    {
        Span<byte> bytes = MemoryMarshal.AsBytes(span);
        _writer.Write(bytes);
    }

    private bool WriteNullableType<T>(Array array)
        where T : unmanaged
    {
        if (array.GetType().GetElementType()! != typeof(T?))
        {
            return false;
        }
        GetNullableColumn((T?[])array, out Span<T> outArray, out Span<nuint> nullableArray);
        WriteValueType(outArray);
        WriteValueType(nullableArray);
        return true;
    }

    private unsafe void GetNullableColumn<T>(T?[] array, out Span<T> outArray, out Span<nuint> nullableArray)
        where T : unmanaged
    {
        nullableArray = new nuint[array.Length / sizeof(nuint)];
        outArray = new T[array.Length];
        int written = 0;
        for (int i = 0; i < array.Length; i++)
        {
            var value = array[i];
            int byteIdx = i / sizeof(nuint);
            int bitIdx = i % sizeof(nuint);
            if (value.HasValue)
            {
                nullableArray[byteIdx] |= (byte)(1 << bitIdx);
                outArray[written++] =  value.Value;
            }
        }
        outArray = outArray[..written];
    }
    

    private bool WriteStrings(Array array)
    {
        if (array.GetType().GetElementType() != typeof(string))
        {
            return false;
        }
        
        foreach (string str in array.Cast<string>())
        {
            _writer.Write(str ?? "");
        }

        return true;
    }
    
    public void Dispose()
    {
        _writer.Dispose();
        _stream.Dispose();
    }
}