using Newtonsoft.Json;
using System.Text;

namespace DataTransfer.Client;

public class Helper
{
    public static T? DeserializeFromStream<T>(MemoryStream stream)
    {
        stream.Position = 0;

        using StreamReader sr = new(stream);
        using JsonReader reader = new JsonTextReader(sr);
        JsonSerializer serializer = new();
        return serializer.Deserialize<T>(reader);
    }

    public static MemoryStream ObjectToMemoryStream(object obj)
    {
        var jsonString = JsonConvert.SerializeObject(obj);
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var memoryStream = new MemoryStream(bytes);
        return memoryStream;
    }
}
