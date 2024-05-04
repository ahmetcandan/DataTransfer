using DataTransfer.Common;

namespace DataTransfer.Client;

public class ResponseQueue
{
    private readonly Dictionary<Guid, Response> _values = [];

    public Response? GetResponse(Guid requestId)
    {
        var exists = _values.ContainsKey(requestId);
        if (!exists)
            return null;

        var response = _values[requestId];
        _values.Remove(requestId);
        return response;
    }

    public void PushQueue(Response response) => _values.Add(response.RequestId, response);

    public void Clear() => _values.Clear();

    public int Count => _values.Count;
}
