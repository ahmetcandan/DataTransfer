namespace DataTransfer.Common;

[Serializable]
public class Response(Guid requestId, string data)
{
    public Guid RequestId { get; private set; } = requestId;
    public string Data { get; set; } = data;
}
