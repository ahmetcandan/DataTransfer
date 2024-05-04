namespace DataTransfer.Client;

[Serializable]
public class Response
{
    public required Guid RequestId { get; set; }
    public required string ResponseData { get; set; }
}
