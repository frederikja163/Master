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
    private readonly ColumnBuilder _builder;

    internal BlobBuilder(ColumnBuilder builder)
    {
        _builder = builder;
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
                _builder.WriteRaw(value);
                break;
        }
    }

    private void WriteBlob(ReadOnlySpan<byte> bytes)
    {
        _builder.WriteRaw(bytes.Length);
        _builder.WriteRaw(bytes, 0);
    }
    public void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        _builder.WriteRaw(bytes, 0);
    }

    public void Dispose()
    {
        _builder.CloseBlob();
    }
}