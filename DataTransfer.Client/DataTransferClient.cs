using DataTransfer.Common;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DataTransfer.Client;

public class DataTransferClient : IDisposable
{
    public DataTransferClient(string serverIPAddress, int serverPort)
    {
        _serverIpAddress = serverIPAddress;
        _serverPort = serverPort;
        Connect();
    }

    public DataTransferClient(string serverIPAddress, int serverPort, int timeout)
    {
        _serverIpAddress = serverIPAddress;
        _serverPort = serverPort;
        _timeoutMiliseconds = timeout;
        Connect();
    }

    private Socket? _socket;
    private NetworkStream? _networkStream;
    private BinaryWriter? _binaryWriter;
    private BinaryReader? _binaryReader;
    private Thread? _thread;
    private volatile bool _working = false;
    private readonly string _serverIpAddress;
    private readonly int _serverPort;
    private readonly int _timeoutMiliseconds = 30000;
    private readonly ResponseQueue _responseQueue = new();

    private bool Connect()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipEndPoint = new(IPAddress.Parse(_serverIpAddress), _serverPort);
            _socket.Connect(ipEndPoint);
            _networkStream = new NetworkStream(_socket);
            _binaryReader = new BinaryReader(_networkStream, Encoding.BigEndianUnicode);
            _binaryWriter = new BinaryWriter(_networkStream, Encoding.BigEndianUnicode);
            _thread = new Thread(new ThreadStart(TRun));
            _working = true;
            _thread.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Disconnect()
    {
        try
        {
            Thread.Sleep(100);
            _working = false;
            _responseQueue.Clear();
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
            _thread?.Join();
            _binaryReader?.Close();
            _binaryWriter?.Close();
            _networkStream?.Close();
        }
        catch { }
    }

    private void TRun()
    {
        while (_working)
        {
            try
            {
                var json = _binaryReader?.ReadString() ?? string.Empty;
                var messageData = MessageData.JsonToMessageData(json);
                if (messageData is not null && messageData.MessageType is MessageType.Response && messageData.Data is JObject jObject)
                    ReceivedResponseHandle(jObject);
            }
            catch
            {
                break;
            }
        }
        _working = false;
    }

    public Response? SendRequest(Request request)
    {
        SendData(MessageData.NewRequest(request));

        var timer = new Stopwatch();
        timer.Start();
        while (true)
        {
            if (timer.ElapsedMilliseconds >= _timeoutMiliseconds)
                return new Response(Guid.Empty, "Timeout");

            var response = _responseQueue.GetResponse(request.Id);
            if (response is not null)
                return response;
        }
    }

    private bool SendData(MessageData messageData)
    {
        try
        {
            if (_binaryWriter is null || _networkStream is null)
                return false;

            _binaryWriter.Write(messageData.ToJson());
            _networkStream.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ReceivedResponseHandle(JObject data)
    {
        var response = data.ToObject<Response>();
        if (response is not null)
            _responseQueue.PushQueue(response.RequestId ,response);
    }

    public void Dispose()
    {
        Disconnect();
    }
}
