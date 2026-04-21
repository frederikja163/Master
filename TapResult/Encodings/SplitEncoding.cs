using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TapResult.Columns;
using TapResult.Readers;

namespace TapResult.Encodings;

/// <summary>
/// Split encoding takes variable length columns and splits the length and the data into separate columns.
/// </summary>
public sealed class SplitEncoding : IEncoding
{
    public EncodingType Type { get; } = EncodingType.Split;
    public IColumn Encode<T>(IColumnReader<T> reader) where T : notnull
    {
        ColumnBuilder lengthBuilder = new ColumnBuilder(LogicalType.SInt32, reader.Length * Unsafe.SizeOf<int>());
        ColumnBuilder byteBuilder = new ColumnBuilder(LogicalType.UInt8, reader.Length - lengthBuilder.PhysicalSize);
        for (int i = 0; i < reader.Length; i++)
        {
            byte[] values;
            if (reader is IColumnReader<byte[]> bReader)
            {
                values = bReader.Read();
            }
            else if (reader is IColumnReader<string> strReader)
            {
                values = Encoding.UTF8.GetBytes(strReader.Read());
            }
            else
            {
                throw new UnreachableException();
            }
            lengthBuilder.WriteValue(values.Length);
            byteBuilder.WriteValues(values);
        }

        return new SplitColumn(lengthBuilder.BuildDataColumn(), byteBuilder.BuildDataColumn(), typeof(T).ToLogicalType());
    }

    public IColumnReader CreateDecoder(LogicalType type, int length,
        GenericReader metadataReader, IEnumerable<IColumnReader> childColumns)
    {
        using IEnumerator<IColumnReader> childColumnEnumerator = childColumns.GetEnumerator();
        if (!childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<int> lengths ||
            !childColumnEnumerator.MoveNext() || childColumnEnumerator.Current is not IColumnReader<byte> bytes ||
            childColumnEnumerator.MoveNext())
            throw new Exception("Child columns not configured correctly for split column.");
        return new SplitColumnReader(lengths, bytes, type);
    }

    public IEnumerable<LogicalType> GetSupportedTypes()
    {
        yield return LogicalType.Blob;
        yield return LogicalType.String;
    }
}