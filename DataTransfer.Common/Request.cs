namespace DataTransfer.Common;

[Serializable]
public class Request(string data)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RequestData { get; set; } = data;
}
