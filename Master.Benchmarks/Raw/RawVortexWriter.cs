using Vortex.Net;

namespace Master.Benchmarks.Raw;

public sealed class RawVortexWriter : IRawBenchmark
{
    public void Write(string path, Data data)
    {
        VxError error = VxError.Zero;
        using VxSession session = new VxSession();
        
        VxStructFieldsBuilder builder = new VxStructFieldsBuilder();
        foreach (var (name, values) in data.ColumnNames.Zip(data.Columns))
        {
            VxString vxString = new VxString(name);
            VxDType vxDType = ArrayToDType(values);
            builder.AddField(vxString, vxDType);
        }

        VxStructFields fields = builder.Finalize();
        VxDType type = Vx.NewStruct(fields, false);
        using VxArraySink sink = session.OpenFile(path, type, ref error);
        error.Dispose();

        for (int i = 0; i < data.Repeats; i++)
        {
            VxArray[] arrays = data.Columns.Select(ArrayToVxArray).ToArray();
            VxArray array = Vx.ArrayStructNew(type, arrays[0], (nuint)arrays.Length, IntPtr.Zero, ref error);
            error.Dispose();
            sink.Push(array, ref error);
            error.Dispose();
        }
    }

    private VxDType ArrayToDType(Array array)
    {
        Type type = array.GetType().GetElementType()!;
        return type == typeof(int) ? Vx.NewPrimitive(VxPType.I32, false) :
            type == typeof(float) ? Vx.NewPrimitive(VxPType.F32, false) :
            type == typeof(double) ? Vx.NewPrimitive(VxPType.F64, false) :
            type == typeof(string) ? Vx.NewUtf8(false) : throw new NotImplementedException();
    }

    private VxArray ArrayToVxArray(Array array)
    {
        Type type = array.GetType().GetElementType()!;

        if (type == typeof(int))
            return new VxArray((int[])array);
        if (type == typeof(float))
            return new VxArray((float[])array);
        if (type == typeof(double))
            return new VxArray((double[])array);

        if (type == typeof(string))
        {
            VxError error =  VxError.Zero;
            VxVarBinViewBuilder builder = Vx.ArrayUtf8BuilderNew(false);
            foreach (object obj in array)
            {
                string str = obj.ToString()!;
                builder.AppendUtf8(str, (nuint)str.Length, ref error);
                error.Dispose();
            }

            return builder.Finish();
        }
        
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return "Vortex";
    }
}