using System.Runtime.InteropServices;

namespace Master.IO;

public class Writer(Stream stream)
{
    private BinaryWriter writer = new (stream);
    
    public void Write(int[] buffer)
    {
        int typeSize = sizeof(int);
        writer.Write(buffer.Length);
        writer.Write(buffer.Length * typeSize);
        byte[] sequence = new byte[buffer.Length * typeSize];

        Buffer.BlockCopy(buffer, 0, sequence, 0, sequence.Length);
        writer.Write(sequence);
    }
    public void Write(Array buffer)
    {
        int typeSize = Marshal.SizeOf(buffer.GetValue(0) ?? 0);
        writer.Write(buffer.Length);
        writer.Write(buffer.Length * typeSize);
        byte[] sequence = new byte[buffer.Length * typeSize];

        Buffer.BlockCopy(buffer, 0, sequence, 0, sequence.Length);
        writer.Write(sequence);
    }

    public void Flush()
    {
        writer.Flush();
    }
}