using DataTransfer.Common;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DataTransfer.Client;

public delegate void OnMessageResponse(Response response);
public delegate void OnMessageRequest(Request request);

public class DataTransferClient(string serverIPAddress, int serverPort)
{
    Socket? _socket;
    NetworkStream? _networkStream;
    BinaryWriter? _binaryWriter;
    BinaryReader? _binaryReader;
    Thread? _thread;
    volatile bool _working = false;
    readonly string _serverIpAddress = serverIPAddress;
    readonly int _serverPort = serverPort;
    readonly int _timeoutMiliseconds = 5000;
    readonly ResponseQueue _responseQueue = new();

    public bool Connect()
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

    public void Disconnected()
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

    bool SendData(MessageData messageData)
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

    void ReceivedResponseHandle(JObject data)
    {
        var response = data.ToObject<Response>();
        if (response is not null)
            _responseQueue.PushQueue(response);
    }
}
