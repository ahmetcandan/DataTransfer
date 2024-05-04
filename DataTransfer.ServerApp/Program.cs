using DataTransfer.Common;
using DataTransfer.Server;

Console.WriteLine("Hello ServerApp!");
DataTransferServer dataTransferServer = new(5004)
{
    OnMessageRequestEvent = (Request request, SendResponse sendResponse) =>
    {
        switch (request.Path)
        {
            case "/test":
                sendResponse(new Response(request.Id, $"Path: {request.Path}, Data: {request.Data}"));
                break;
            default:
                sendResponse(new Response(request.Id, "path bulunamadı"));
                break;
        }
    }
};
Console.ReadLine();