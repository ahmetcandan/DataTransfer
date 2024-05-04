using DataTransfer.Common;
using static DataTransfer.Server.DataTransferServer;

namespace DataTransfer.Server;

internal class RequestForClient(Client client, string path, string data) : Request(path, data)
{
    private readonly Client _client = client;
}
