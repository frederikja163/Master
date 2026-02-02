
namespace Master.Tests;
using IO;

public class ReaderWriterTests
{
    [Test]
    public void Test()
    {
        int[] array = [2312312,124124,124124,123123123,124124,1254125];
        MemoryStream stream = new();
        Writer writer = new(stream);
        writer.Write(array);
        
        writer.Flush();
        stream.Position = 0;

        Reader reader = new(stream);

         int[] newArray = reader.Read<int>();
         Assert.That(newArray, Is.EquivalentTo(array));
    }
}