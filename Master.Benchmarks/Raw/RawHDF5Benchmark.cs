using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using HDF.PInvoke;

namespace Master.Benchmarks.Raw;


internal sealed class RawHdf5Benchmark : IRawBenchmark
{
    public unsafe void Write(string path, Data data)
    {
        using Disposable<long> fileId = new Disposable<long>(H5F.create(path, H5F.ACC_TRUNC), i => H5F.close(i)); 
        Debug.Assert(fileId > 0);

        using Disposable<long> groupId = new Disposable<long>(H5G.create(fileId, data.ToString()), i => H5G.close(i));
        Debug.Assert(groupId > 0);

        ulong dims = 0ul;
        ulong maxDims = H5S.UNLIMITED;
        using Disposable<long> spaceId = new Disposable<long>(H5S.create_simple(1, &dims, &maxDims), i => H5S.close(i));
        Debug.Assert(spaceId > 0);
        
        using Disposable<long> datasetCreate = new Disposable<long>(H5P.create(H5P.DATASET_CREATE), i => H5P.close(i));
        Debug.Assert(datasetCreate > 0);

        ulong chunkSize = 10_000;
        H5P.set_chunk(datasetCreate, 1, &chunkSize);
        using DisposableList<long> datasets = data.ColumnNames.Zip(data.Columns)
            .Select(t => H5D.create(groupId, t.First, GetType(t.Second.GetType().GetElementType()!), spaceId, H5P.DEFAULT, datasetCreate))
            .ToDisposableList(i => H5D.close(i));
        Debug.Assert(datasets.All(d => d > 0));
        
        for (int i = 0; i < data.Repeats; i++)
        {
            ulong start = dims;
            ulong count = (ulong)data.Count;
            dims = start + count;
            foreach ((long datasetId, Array values) in datasets.Zip(data.Columns))
            {
                H5D.set_extent(datasetId, &dims);
                using Disposable<long> filespace = new Disposable<long>(H5D.get_space(datasetId), id => H5S.close(id));
                H5S.select_hyperslab(filespace, H5S.seloper_t.SET, &start, null, &count, null);

                using Disposable<long> memspace = new Disposable<long>(H5S.create_simple(1, &count, null), id => H5S.close(id));
                
                using Disposable<GCHandle> handle = GetValues(values);
                IntPtr ptr = handle.Value.AddrOfPinnedObject();
                H5D.write(datasetId, GetType(values.GetType().GetElementType()!), memspace, filespace, H5P.DEFAULT, ptr);
            }
        }
    }

    public static long GetType(Type type)
    {
        if (type == typeof(int))
        {
            return H5T.NATIVE_INT32;
        }

        if (type == typeof(float))
        {
            return H5T.NATIVE_FLOAT;
        }

        if (type == typeof(double))
        {
            return H5T.NATIVE_DOUBLE;
        }

        if (type == typeof(string))
        {
            long typeId = H5T.copy(H5T.C_S1);
            H5T.set_size(typeId, H5T.VARIABLE);
            H5T.set_cset(typeId, H5T.cset_t.UTF8);
            return typeId;
        }

        throw new ArgumentOutOfRangeException(nameof(type));
    }

    public static Disposable<GCHandle> GetValues(Array values)
    {
        if (values.GetType().GetElementType()! != typeof(string))
        {
            return new Disposable<GCHandle>(GCHandle.Alloc(values, GCHandleType.Pinned), h => h.Free());
        }
        IntPtr[] ptrs = values.Cast<string>().Select(Marshal.StringToCoTaskMemUTF8).ToArray();
        return new Disposable<GCHandle>(GCHandle.Alloc(ptrs, GCHandleType.Pinned), h =>
        {
            h.Free();
            foreach (IntPtr ptr in ptrs)
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        });
    }

    public override string ToString()
    {
        return "Hdf5";
    }
}