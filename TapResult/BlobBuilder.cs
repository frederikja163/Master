using System.Text;

namespace TapResult;


/// <summary>
/// Writes blobs to a <see cref="ColumnBuilder"/>.
/// Created using <see cref="ColumnBuilder.OpenBlob"/>
/// </summary>
public interface IBlobBuilder
{
    /// <summary>
    /// Write values to this Blob.
    /// </summary>
    public void WriteValue<T>(T value);

    /// <summary>
    /// Writes an amount of bytes to the blob without a length prefix.
    /// If you want your blob to contain another blob use <see cref="WriteValue{T}"/>
    /// </summary>
    public void WriteRaw(ReadOnlySpan<byte> bytes);
}

/// <summary>
/// Writes blobs to a <see cref="ColumnBuilder"/>.
/// Created using <see cref="ColumnBuilder.OpenBlob"/>
/// </summary>
public sealed class BlobBuilder : IBlobBuilder, IDisposable
{
    private readonly IRawWriter _writer;

    internal BlobBuilder(IRawWriter writer)
    {
        _writer = writer;
    }
    
    internal int StartIndex { get; init; }

    public void WriteValue<T>(T value)
    {
        switch (value)
        {
            case string str:
                WriteBlob(Encoding.UTF8.GetBytes(str));
                break;
            case byte[] blob:
                WriteBlob(blob);
                break;
            default:
                _writer.WriteRaw(value);
                break;
        }
    }

    private void WriteBlob(ReadOnlySpan<byte> bytes)
    {
        _writer.WriteRaw(bytes.Length);
        _writer.WriteRaw(bytes);
    }
    public void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        _writer.WriteRaw(bytes);
    }

    public void Dispose()
    {
        _writer.CloseBlob();
    }
}