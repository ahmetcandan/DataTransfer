using System.Net.Sockets;
using System.Net;
using DataTransfer.Client;

namespace DataTransfer.Server;

public class DataTransferServer(int port)
{
    private TcpListener? _listenerSocket;
    private readonly int _port = port;
    private volatile bool _working = false;
    private volatile Thread? _thread;
    DataTransferClient? _client;

    public bool Start()
    {
        if (Connect())
        {
            _working = true;
            _thread = new Thread(new ThreadStart(TListen));
            _thread.Start();
            return true;
        }
        else
            return false;
    }

    public bool Stop()
    {
        try
        {
            _working = false;
            Disconnect();
            _thread?.Join();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool Connect()
    {
        try
        {
            _listenerSocket = new TcpListener(IPAddress.Any, _port);
            _listenerSocket.Start();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private bool Disconnect()
    {
        try
        {
            if (_listenerSocket is null)
                return true;

            _listenerSocket.Stop();
            _listenerSocket = null;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void TListen()
    {
        Socket clientSocket;
        while (_working)
        {
            try
            {
                if (_listenerSocket is not null)
                {
                    clientSocket = _listenerSocket.AcceptSocket();
                    if (clientSocket.Connected)
                    {

                    }
                }
            }
            catch (Exception)
            {
                if (_working)
                {
                    Disconnect();
                    try { Thread.Sleep(1000); }
                    catch (Exception) { }
                    Connect();
                }
            }
        }
    }

    void OnBeginAccept(IAsyncResult asyncResult)
    {
        Socket socket = _socket.EndAccept(asyncResult);
        _client = new(socket);

        _client.OnMessageRequestEvent += new OnMessageRequest(OnReceivedRequest);
        _client.Start();

        _socket.BeginAccept(OnBeginAccept, null);
    }

    void OnReceivedRequest(Request request)
    {
        var response = new Response { ResponseData = "[Response]" + request.RequestData, RequestId = request.Id };
        _client?.SendResponse(response);
    }
}
