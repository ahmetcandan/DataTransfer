using Newtonsoft.Json;
using System.Text;

namespace DataTransfer.Common;

public static class Helper
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

    public static string ToJson(this MessageData messageData) => JsonConvert.SerializeObject(messageData);
    public static MessageData? JsonToMessageData(this string json) => JsonConvert.DeserializeObject<MessageData>(json);
}
