using DataTransfer.Server;

Console.WriteLine("Hello ServerApp!");
DataTransferServer dataTransferServer = new(5004);
dataTransferServer.Start();

Console.ReadLine();
dataTransferServer.Stop();