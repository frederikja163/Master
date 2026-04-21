using System.Diagnostics.CodeAnalysis;

namespace TapResult.Readers;

/// <summary>
/// The base of a column reader. Most likely you want to cast this to a <see cref="IColumnReader{T}"/> of the correct type.
/// </summary>
public interface IColumnReader
{
    /// <summary>
    /// Whether this IColumnReader is at the end of its source.
    /// </summary>
    public bool IsAtEnd => Index >= Length;
    /// <summary>
    /// The number of elements in this column reader.
    /// </summary>
    public int Length { get; }
    /// <summary>
    /// The current index of the column reader.
    /// </summary>
    public int Index { get; }
    /// <summary>
    /// The <see cref="LogicalType"/> of this reader.
    /// </summary>
    public LogicalType Type { get; }
    /// <summary>
    /// Advance the column reader an amount of units in its source.
    /// </summary>
    public void Advance(int units);
    
    /// <summary>
    /// Peek the next value, or a value with some offset. Does not consume.
    /// </summary>
    public object? Peek(int offset = 0);
    
    /// <summary>
    /// Peeks multiple values with some offset. Does not consume.
    /// </summary>
    public IEnumerable<object?> Peek(int offset, int count);

    /// <summary>
    /// Clone this column reader at the current index.
    /// </summary>
    public IColumnReader Clone();
}

/// <summary>
/// A column reader for a known type.
/// </summary>
public interface IColumnReader<out T> : IColumnReader
{
    /// <summary>
    /// Peek the next value, or a value with some offset. Does not consume.
    /// </summary>
    public new T Peek(int offset = 0);
    /// <summary>
    /// Peeks multiple values with some offset. Does not consume.
    /// </summary>
    public new IEnumerable<T> Peek(int offset, int count);

    /// <summary>
    /// Clone this column reader at the current index.
    /// </summary>
    public new IColumnReader<T> Clone();
}

/// <summary>
/// Extensions for <see cref="IColumnReader{T}"/>
/// </summary>
public static class ColumnReaderExtensions {
    /// <summary>
    /// Reads the next value from the data and advances the reader by one.
    /// </summary>
    public static T Read<T>(this IColumnReader<T> reader)
    {
        T value = reader.Peek();
        reader.Advance(1);
        return value;
    }

    /// <summary>
    /// Reads multiple values from the data and advances the reader.
    /// </summary>
    public static IEnumerable<T> Read<T>(this IColumnReader<T> reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return reader.Read();
        }
    }
    
    /// <summary>
    /// Reads the next value from the data and advances the reader by one.
    /// </summary>
    public static object? Read(this IColumnReader reader)
    {
        object? value = reader.Peek();
        reader.Advance(1);
        return value;
    }

    /// <summary>
    /// Reads multiple values from the data and advances the reader.
    /// </summary>
    public static IEnumerable<object?> Read(this IColumnReader reader, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return reader.Read();
        }
    }
    
    /// <summary>
    /// Tries to convert an <see cref="IColumnReader{T}"/> into another compatible <see cref="IColumnReader{T}"/>.
    /// Compatibility is defined by <see cref="TypeHelper.IsCompatible"/>.
    /// </summary>
    public static bool TryConvertReader<T>(this IColumnReader inReader, [NotNullWhen(true)] out IColumnReader<T>? outReader)
    {
        Type outType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (inReader.Type.IsCompatible(outType.ToLogicalType()) || inReader is not IColumnReader<T> reader)
        {
            outReader = null;
            return false;
        }

        outReader = reader;
        return true;
    }
}