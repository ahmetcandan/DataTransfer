using DataTransfer.Common;

namespace DataTransfer.Client;

public class ResponseQueue
{
    private readonly Dictionary<Guid, Response> _values = [];

    public Response? GetResponse(Guid key)
    {
        var exists = _values.ContainsKey(key);
        if (!exists)
            return null;

        var response = _values[key];
        _values.Remove(key);
        return response;
    }

    public void PushQueue(Guid key, Response response) => _values.Add(key, response);

    public void Clear() => _values.Clear();

    public int Count => _values.Count;
}
