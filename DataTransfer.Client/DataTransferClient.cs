using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DataTransfer.Client;

public delegate void OnMessageResponse(Response response);
public delegate void OnMessageRequest(Request request);

public class DataTransferClient(string serverIPAddress, int serverPort)
{
    public OnMessageResponse? OnMessageResponseEvent;
    public OnMessageRequest? OnMessageRequestEvent;
    Socket? _socket;
    NetworkStream? _networkStream;
    BinaryWriter? _binaryWriter;
    BinaryReader? _binaryReader;
    Thread? _thread;
    volatile bool _working = false;
    readonly string _serverIpAddress = serverIPAddress;
    readonly int _serverPort = serverPort;
    readonly int _timeoutMiliseconds = 10000;
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
        catch (Exception)
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
            _socket?.Close();
            _thread?.Join();
        }
        catch (Exception)
        {

        }
    }

    private void TRun()
    {
        while (_working)
        {
            try
            {
                var result = _binaryReader?.ReadString() ?? string.Empty;
                if (!string.IsNullOrEmpty(result))
                    HandleReceivedData(result);
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
        var byteArray = Encoding.UTF8.GetBytes('0' + JsonConvert.SerializeObject(request));
        SendData(byteArray);

        var timer = new Stopwatch();
        timer.Start();
        while (true)
        {
            if (timer.ElapsedMilliseconds >= _timeoutMiliseconds)
                return new Response() { ResponseData = "Timeout", RequestId = Guid.Empty };

            var response = _responseQueue.GetResponse(request.Id);
            if (response is not null)
                return response;
        }
    }

    public void SendResponse(Response response)
    {
        var byteArray = Encoding.UTF8.GetBytes('1' + JsonConvert.SerializeObject(response));
        SendData(byteArray);
    }

    bool SendData(byte[] data)
    {
        try
        {
            if (_binaryWriter is null || _networkStream is null)
                return false;

            _binaryWriter.Write(data);
            _networkStream.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    void HandleReceivedData(string result)
    {
        var s = result[0];
        var json = result[1..];
        if (s == '0' && OnMessageRequestEvent is not null)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<Request>(json);
                if (request is not null)
                    OnMessageRequestEvent(request);
            }
            catch { }
        }
        else if (s == '1' && OnMessageResponseEvent is not null)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<Response>(json);
                if (response is not null)
                {
                    _responseQueue.PushQueue(response);
                    OnMessageResponseEvent(response);
                }
            }
            catch { }
        }
    }
}
