using System.Runtime.CompilerServices;
using Master.Serializing;

namespace Master.Benchmarks;

internal sealed class FixedDataColumnBuilder : DataColumnBuilderBase
{
    public FixedDataColumnBuilder(int size) : base(size)
    {
    }

    public FixedDataColumnBuilder(LogicalType type, int size) : base(type, size)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected override Span<byte> Slice(int size)
    {
        if ((uint)_index + size > (uint)_data.Length)
        {
            throw new IndexOutOfRangeException();
        }

        Span<byte> slice = _data.Span.Slice(_index, size);
        _index += size;
        return slice;
    }
}