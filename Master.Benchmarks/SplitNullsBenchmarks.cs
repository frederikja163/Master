#define original
using System.Collections;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Master.Benchmarks.Extensions;
using Master.Serializing;

namespace Master.Benchmarks;

[MemoryDiagnoser]
public class SplitNullsBenchmarks
{
    private int?[] array = [];
    private DataColumn? nulls = DataColumn.Empty;

    [GlobalSetup]
    public void Setup()
    {
        array = Enumerable.Range(0, 10_000).Select(_ => Random.Shared.Next()).WithNullsStruct(0.5f).ToArray();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn BitArray()
    {
        int valueSize = 0;
        foreach (var value in array)
        {
            if (value is null)
            {
                continue;
            }

            valueSize += Unsafe.SizeOf<int>();
        }
        
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), valueSize, false);
        BitArray nullArray = new BitArray(array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            if (value is { } val)
            {
                nullArray[i] = true;
                valueBuilder.Write(val);
            }
            else
            {
                nullArray[i] = false;
            }
        }

        int[] nullsInt = new int[(array.Length + 31) / 32];
        nullArray.CopyTo(nullsInt, 0);
        nulls = DataColumn.Create<int>(nullsInt);
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn Unrolled()
    {
        int valueSize = 0;
        foreach (var value in array)
        {
            if (value is null)
            {
                continue;
            }

            valueSize += Unsafe.SizeOf<int>();
        }
        
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), valueSize, false);
        int size = array.Length / 8 + 1;
        var nullBuilder = new DataColumnBuilder(LogicalType.UInt8, size, false);
        int i = 0;
        for (; i + 8 < array.Length; i += 8)
        {
            byte nullByte = 0;
            Iteration(array[i], valueBuilder, ref nullByte);
            Iteration(array[i+1], valueBuilder, ref nullByte);
            Iteration(array[i+2], valueBuilder, ref nullByte);
            Iteration(array[i+3], valueBuilder, ref nullByte);
            Iteration(array[i+4], valueBuilder, ref nullByte);
            Iteration(array[i+5], valueBuilder, ref nullByte);
            Iteration(array[i+6], valueBuilder, ref nullByte);
            Iteration(array[i+7], valueBuilder, ref nullByte);

            nullBuilder.Write(nullByte);
        }

        byte b = 0;
        for (; i < array.Length; i++)
        {
            Iteration(array[i], valueBuilder, ref b);
        }
        nullBuilder.Write(b);
        
        nulls = nullBuilder.Build();
        return valueBuilder.Build();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Iteration(int? value, DataColumnBuilder dataColumnBuilder, ref byte nullByte)
        {
            if (value is { } val)
            {
                nullByte = (byte)((nullByte << 1) | 0);
                dataColumnBuilder.Write(val);
            }
            else
            {
                nullByte = (byte)((nullByte << 1) | 1);
            }
        }
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn Array()
    {
        int valueSize = 0;
        foreach (var value in array)
        {
            if (value is null)
            {
                continue;
            }

            valueSize += Unsafe.SizeOf<int>();
        }
        
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), valueSize, false);
        byte[] nullBuilder = new byte[array.Length / 8 + 1];
        byte nullByte = 0;
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            if (value is { } val)
            {
                nullByte = (byte)((nullByte << 1) | 0);
                valueBuilder.Write(val);
            }
            else
            {
                nullByte = (byte)((nullByte << 1) | 1);
            }

            if ((i & 7) == 7)
            {
                nullBuilder[i / 8] = nullByte;
                nullByte = 0;
            }
        }

        nullBuilder[nullBuilder.Length - 1] = nullByte;
        
        nulls = DataColumn.Create<byte>(nullBuilder);
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn Default()
    {
        int valueSize = 0;
        foreach (var value in array)
        {
            if (value is null)
            {
                continue;
            }

            valueSize += Unsafe.SizeOf<int>();
        }
        
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), valueSize, false);
        int size = array.Length / 8 + 1;
        var nullBuilder = new DataColumnBuilder(LogicalType.UInt8, size, false);
        byte nullByte = 0;
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            if (value is { } val)
            {
                nullByte = (byte)((nullByte << 1) | 0);
                valueBuilder.Write(val);
            }
            else
            {
                nullByte = (byte)((nullByte << 1) | 1);
            }

            if ((i & 7) == 7)
            {
                nullBuilder.Write(nullByte);
                nullByte = 0;
            }
        }
        nullBuilder.Write(nullByte);
        
        nulls = nullBuilder.Build();
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn SingleLoopResizeable()
    {
        int size = array.Length / 10;
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), size, true);
        int size1 = array.Length / 8 + 1;
        var nullBuilder = new DataColumnBuilder(LogicalType.UInt8, size1, false);
        byte nullByte = 0;
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            if (value is { } val)
            {
                nullByte = (byte)((nullByte << 1) | 0);
                valueBuilder.Write(val);
            }
            else
            {
                nullByte = (byte)((nullByte << 1) | 1);
            }

            if ((i & 7) == 7)
            {
                nullBuilder.Write(nullByte);
                nullByte = 0;
            }
        }
        nullBuilder.Write(nullByte);
        
        nulls = nullBuilder.Build();
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn SingleLoopOversized()
    {
        int size = array.Length * Unsafe.SizeOf<int>();
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), size, false);
        int size1 = array.Length / 8 + 1;
        var nullBuilder = new DataColumnBuilder(LogicalType.UInt8, size1, false);
        byte nullByte = 0;
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            if (value is { } val)
            {
                nullByte = (byte)((nullByte << 1) | 0);
                valueBuilder.Write(val);
            }
            else
            {
                nullByte = (byte)((nullByte << 1) | 1);
            }

            if ((i & 7) == 7)
            {
                nullBuilder.Write(nullByte);
                nullByte = 0;
            }
        }
        nullBuilder.Write(nullByte);
        
        nulls = nullBuilder.Build();
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn SingleLoopOversizedBranchless()
    {
        int size = array.Length * Unsafe.SizeOf<int>();
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), size, false);
        int size1 = array.Length / 8 + 1;
        var nullBuilder = new DataColumnBuilder(LogicalType.UInt8, size1, false);
        byte nullByte = 0;
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            byte nullable = value.HasValue ? (byte)0 : (byte)1;
            nullByte = (byte)(nullByte << 1 | nullable);
            valueBuilder.Write(value.GetValueOrDefault());

            if ((i & 7) == 7)
            {
                nullBuilder.Write(nullByte);
                nullByte = 0;
            }
        }
        nullBuilder.Write(nullByte);
        
        nulls = nullBuilder.Build();
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn SingleLoopOversizedBitArray()
    {
        int size = array.Length * Unsafe.SizeOf<int>();
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), size, false);
        var nullBuilder = new BitArray(array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            nullBuilder[i] = value.HasValue;
            if (value.HasValue)
            {
                valueBuilder.Write(value.GetValueOrDefault());
            }
        }

        int[] ints = new int[(nullBuilder.Length + 31) / 32];
        nullBuilder.CopyTo(ints, 0);
        nulls = DataColumn.Create<int>(ints);
        return valueBuilder.Build();
    }
    
    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public DataColumn SingleLoopOversizedBranchlessBitArray()
    {
        int size = array.Length * Unsafe.SizeOf<int>();
        var valueBuilder = new DataColumnBuilder(typeof(int).ToLogicalType(), size, false);
        var nullBuilder = new BitArray(array.Length);
        for (int i = 0; i < array.Length; i++)
        {
            int? value = array[i];
            nullBuilder[i] = value.HasValue;
            valueBuilder.Write(value.GetValueOrDefault());
        }

        int[] ints = new int[(nullBuilder.Length + 31) / 32];
        nullBuilder.CopyTo(ints, 0);
        nulls = DataColumn.Create<int>(ints);
        return valueBuilder.Build();
    }
}