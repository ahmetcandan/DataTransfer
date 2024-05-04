using Newtonsoft.Json;

namespace DataTransfer.Common;

public class MessageData(object data, MessageType messageType)
{
    public object Data { get; set; } = data;
    public MessageType MessageType { get; set; } = messageType;

    public static MessageData NewRequest(Request request) => new(request, MessageType.Request);
    public static MessageData NewResponse(Response response) => new(response, MessageType.Response);
    public static MessageData? JsonToMessageData(string json) => JsonConvert.DeserializeObject<MessageData>(json);
}
