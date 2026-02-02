namespace Master.IO;

public class Reader(Stream stream)
{
    private BinaryReader binaryReader = new BinaryReader(stream);
    
    public void Jump(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var count = binaryReader.ReadInt32();
            var offset = binaryReader.ReadInt32();
            binaryReader.ReadBytes(offset);
        }
    }

    public Type GetType()
    {
        var pos = binaryReader.BaseStream.Position;
        var count = binaryReader.ReadInt32();
        var offset = binaryReader.ReadInt32();
        binaryReader.BaseStream.Position = pos; // avoid advancing reader
        return (offset / count) switch
        {
            1 => typeof(int),
            _ => typeof(byte)
        };
    }
    
    public object[] Read()
    {
        var count = binaryReader.ReadInt32();
        var offset = binaryReader.ReadInt32();
        return (offset / count) switch // offset / count = size of type
        {
            1 => [InternalRead<int>(count, offset)],
            _ => [InternalRead<byte>(count, offset)],
        };
    }

    public T[] Read<T>()
    {
        var count = binaryReader.ReadInt32();
        var offset = binaryReader.ReadInt32();
        return InternalRead<T>(count, offset);
    }

    private T[] InternalRead<T>(int count, int offset)
    {
        byte[] sequence = binaryReader.ReadBytes(offset);

        var numbers = new T[count];
        Buffer.BlockCopy(sequence, 0, numbers, 0, offset);
        return numbers;
    }
}