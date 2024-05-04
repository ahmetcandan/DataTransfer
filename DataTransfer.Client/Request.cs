namespace DataTransfer.Client;

[Serializable]
public class Request
{
    public required Guid Id { get; set; }
    public required string RequestData { get; set; }
}
