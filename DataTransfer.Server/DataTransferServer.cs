using DataTransfer.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DataTransfer.Server;

public delegate void OnMessageRequest(Request request, SendResponse sendResponse);
public delegate bool SendResponse(Response response);

public class DataTransferServer : IDisposable
{
    public DataTransferServer(int port)
    {
        _port = port;
        Start();
    }

    public OnMessageRequest? OnMessageRequestEvent;
    private TcpListener? _tcpListener;
    private readonly int _port;
    private volatile bool _working = false;
    private volatile Thread? _thread;
    private readonly Dictionary<long, Client> _clients = [];
    private readonly object _objSenk = new();
    private long _lastClientId = 0;

    private bool Start()
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

    private bool Stop()
    {
        try
        {
            _working = false;
            Disconnect();
            _thread?.Join();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool Connect()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool Disconnect()
    {
        try
        {
            if (_tcpListener is null)
                return true;

            _tcpListener.Stop();
            _tcpListener = null;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void TListen()
    {
        Socket clientSocket;
        while (_working)
        {
            try
            {
                if (_tcpListener is not null)
                {
                    clientSocket = _tcpListener.AcceptSocket();
                    if (clientSocket.Connected)
                        ClientConnected(clientSocket);
                }
            }
            catch
            {
                if (_working)
                {
                    Disconnect();
                    try { Thread.Sleep(1000); }
                    catch { }
                    Connect();
                }
            }
        }
    }

    private void ClientConnected(Socket clientSocket)
    {
        Client? client = null;
        lock (_objSenk)
        {
            client = new Client(this, clientSocket, ++_lastClientId);
            _clients.Add(client.ClientId, client);
        }
        client?.Start();
    }

    private void ClientDisconnected(long clientId)
    {
        if (_working)
            lock (_objSenk)
                _clients.Remove(clientId);
    }

    private void ReceivedRequest(Client client, JObject data)
    {
        var request = data.ToObject<Request>();
        if (request is not null)
        {
            if (OnMessageRequestEvent is not null)
                OnMessageRequestEvent(request, client.SendResponse);
        }
    }

    public void Dispose() => Stop();

    internal class Client(DataTransferServer server, Socket clientSocket, long clientId)
    {
        public long ClientId { get; } = clientId;
        private readonly DataTransferServer _server = server;
        private readonly Socket _soket = clientSocket;
        private NetworkStream? _networkStram;
        private BinaryReader? _binaryReader;
        private BinaryWriter? _binaryWriter;
        private Thread? _thread;
        private volatile bool _working = false;

        public bool Start()
        {
            try
            {
                _networkStram = new NetworkStream(_soket);
                _binaryReader = new BinaryReader(_networkStram, Encoding.BigEndianUnicode);
                _binaryWriter = new BinaryWriter(_networkStram, Encoding.BigEndianUnicode);
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

        public void Stop()
        {
            try
            {
                _working = false;
                _soket.Close();
                _thread?.Join();
            }
            catch { }
        }

        public bool SendResponse(Response response)
        {
            try
            {
                _binaryWriter?.Write(MessageData.NewResponse(response).ToJson());
                _networkStram?.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void TRun()
        {
            while (_working)
            {
                try
                {
                    string? json = _binaryReader?.ReadString() ?? string.Empty;
                    var messageData = JsonConvert.DeserializeObject<MessageData>(json);
                    if (messageData is not null && messageData.MessageType is MessageType.Request && messageData.Data is JObject jObject)
                        _server.ReceivedRequest(this, jObject);
                }
                catch
                {
                    break;
                }
            }
            _working = false;
            try
            {
                if (_soket.Connected)
                {
                    _soket.Close();
                }
            }
            catch { }
            _server.ClientDisconnected(ClientId);
        }
    }
}
