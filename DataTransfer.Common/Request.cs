namespace DataTransfer.Common;

[Serializable]
public class Request(string path, string data)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Data { get; set; } = data;
    public string Path { get; set; } = path;
}
