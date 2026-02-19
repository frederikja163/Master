using System.Runtime.CompilerServices;
using Master.Serializing;

namespace Master.Benchmarks;

internal sealed class ResizeableDataColumnBuilder : DataColumnBuilderBase
{
    public ResizeableDataColumnBuilder(int size) : base(size)
    {
    }

    public ResizeableDataColumnBuilder(LogicalType type, int size) : base(type, size)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected override Span<byte> Slice(int size)
    {
        while ((uint)_index + size > (uint)_data.Length)
        {
            Memory<byte> oldData = _data;
            _data = new byte[oldData.Length * 2];
            oldData.CopyTo(_data);
        }

        Span<byte> slice = _data.Span.Slice(_index, size);
        _index += size;
        return slice;
    }
}