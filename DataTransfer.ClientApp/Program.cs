using DataTransfer.Client;

Console.WriteLine("Hello ClientApp!");

DataTransferClient dataTransferClient = new("127.0.0.1", 5004);
dataTransferClient.Start();

var request = new Request { RequestData = "test mesajı A", Id = Guid.NewGuid() };
Console.WriteLine($"Request.Id: {request.Id}, Request.Context: {request.RequestData}");
var response = dataTransferClient.SendRequest(request);
if (response is not null)
    Console.WriteLine($"Response.RequestId: {response.RequestId}, Response.Context: {response.ResponseData}");
else
    Console.WriteLine("No response");

var request2 = new Request { RequestData = "test mesajı B", Id = Guid.NewGuid() };
Console.WriteLine($"Request2.Id: {request2.Id}, Request2.Context: {request2.RequestData}");
var response2 = dataTransferClient.SendRequest(request2);
if (response2 is not null)
    Console.WriteLine($"Response.RequestId: {response2.RequestId}, Response.Context: {response2.ResponseData}");
else
    Console.WriteLine("No response");

var request3 = new Request { RequestData = "test mesajı C", Id = Guid.NewGuid() };
Console.WriteLine($"Request2.Id: {request3.Id}, Request2.Context: {request3.RequestData}");
var response3 = dataTransferClient.SendRequest(request3);
if (response3 is not null)
    Console.WriteLine($"Response.RequestId: {response3.RequestId}, Response.Context: {response3.ResponseData}");
else
    Console.WriteLine("No response");

Console.ReadLine();
dataTransferClient.Stop();