using Master.IO;

namespace Master.Benchmarks.Raw;

internal sealed class RawCustom : IRawBenchmark
{
    public void Write(string path, Data data)
    {
        FileStream stream = new FileStream(path, FileMode.Create);
        Writer writer = new Writer(stream);
        for (int i = 0; i < data.Repeats; i++)
        {
            foreach (Array array in data.Columns)
            {
                writer.Write(array);
            }
        }
    }
}