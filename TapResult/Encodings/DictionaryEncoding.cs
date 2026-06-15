using System.Numerics;
using System.Runtime.CompilerServices;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

public sealed class DictionaryEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.Dictionary;

    public IColumn Encode<T>(IColumnReader<T> reader) where T : notnull
    {
        ColumnBuilder<T> dictBuilder = new(reader.Length * Unsafe.SizeOf<T>());
        ColumnBuilder<int> indexBuilder = new(reader.Length * Unsafe.SizeOf<int>());
        Dictionary<T, int> valueToIndex = new();

        for (int i = 0; i < reader.Length; i++)
        {
            T value = reader.Read();
            if (!valueToIndex.TryGetValue(value, out int idx))
            {
                idx = valueToIndex.Count;
                valueToIndex[value] = idx;
                dictBuilder.WriteValue(value);
            }
            indexBuilder.WriteValue(idx);
        }

        return new DictionaryColumn(typeof(T).ToLogicalType(), dictBuilder.Build(), indexBuilder.Build(), reader.Length);
    }

    public IColumnReader CreateDecoder(LogicalType type, int length, GenericReader metadataReader, params IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not { } dictValues ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> indices ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for Dictionary column.");
        return CreateReader(type, dictValues, indices, length);
    }

    internal static IColumnReader CreateReader(LogicalType type, IColumnReader dictReader, IColumnReader<int> indexReader, int length)
    {
        return type switch
        {
            LogicalType.SInt8 => new DictionaryColumnReader<sbyte>(dictReader, indexReader, length, type),
            LogicalType.SInt16 => new DictionaryColumnReader<short>(dictReader, indexReader, length, type),
            LogicalType.SInt32 => new DictionaryColumnReader<int>(dictReader, indexReader, length, type),
            LogicalType.SInt64 => new DictionaryColumnReader<long>(dictReader, indexReader, length, type),
            LogicalType.UInt8 => new DictionaryColumnReader<byte>(dictReader, indexReader, length, type),
            LogicalType.UInt16 => new DictionaryColumnReader<ushort>(dictReader, indexReader, length, type),
            LogicalType.UInt32 => new DictionaryColumnReader<uint>(dictReader, indexReader, length, type),
            LogicalType.UInt64 => new DictionaryColumnReader<ulong>(dictReader, indexReader, length, type),
            LogicalType.Float16 => new DictionaryColumnReader<Half>(dictReader, indexReader, length, type),
            LogicalType.Float32 => new DictionaryColumnReader<float>(dictReader, indexReader, length, type),
            LogicalType.Float64 => new DictionaryColumnReader<double>(dictReader, indexReader, length, type),
            LogicalType.Blob => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            LogicalType.String => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public IEnumerable<LogicalType> GetSupportedTypes() => TypeHelper.AllTypes();
}
