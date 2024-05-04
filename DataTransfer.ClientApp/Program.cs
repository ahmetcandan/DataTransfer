using DataTransfer.Client;
using DataTransfer.Common;
using System.Diagnostics;

Console.WriteLine("Hello ClientApp!");

Thread.Sleep(1000);
DataTransferClient dataTransferClient = new("127.0.0.1", 5004);

var timer = new Stopwatch();
var request1 = new Request("/test", "test mesajı 1");
timer.Start();
Console.WriteLine($"Request1.Id: {request1.Id}, Request.Context: {request1.Data}");
var response = dataTransferClient.SendRequest(request1);
timer.Stop();
timer.Reset();
if (response is not null)
    Console.WriteLine($"Response1: Time: {timer.ElapsedMilliseconds}, RequestId: {response.RequestId}, Context: {response.Data}");
else
    Console.WriteLine("No response");

var request2 = new Request("/test2", "test mesajı 2");
Console.WriteLine($"Request2.Id: {request2.Id}, Request2.Context: {request2.Data}");
timer.Start();
var response2 = dataTransferClient.SendRequest(request2);
timer.Stop();
timer.Reset();
if (response2 is not null)
    Console.WriteLine($"Response2: Time: {timer.ElapsedMilliseconds}, RequestId: {response2.RequestId}, Context: {response2.Data}");
else
    Console.WriteLine("No response");

var request3 = new Request("/test", "test mesajı 3");
Console.WriteLine($"Request3.Id: {request3.Id}, Request2.Context: {request3.Data}");
timer.Start();
var response3 = dataTransferClient.SendRequest(request3);
timer.Stop();
timer.Reset();
if (response3 is not null)
    Console.WriteLine($"Response3: Time: {timer.ElapsedMilliseconds}, RequestId: {response3.RequestId}, Context: {response3.Data}");
else
    Console.WriteLine("No response");

Console.ReadLine();
dataTransferClient.Disconnected();
Console.ReadLine();