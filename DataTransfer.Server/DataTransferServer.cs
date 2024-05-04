using System.Net.Sockets;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using DataTransfer.Common;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks.Dataflow;

namespace DataTransfer.Server;

public class DataTransferServer(int port)
{
    TcpListener? _tcpListener;
    readonly int _port = port;
    volatile bool _working = false;
    volatile Thread? _thread;
    readonly Dictionary<long, Client> _clients = [];
    readonly object _objSenk = new();
    long _lastClientId = 0;

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

    private static void ReceivedRequest(Client client, JObject data)
    {
        var request = data.ToObject<Request>();
        if (request is not null)
            client.SendResponse(new Response(request.Id, $"Request alındı, Response: [{request.RequestData}]..."));
    }

    class Client(DataTransferServer server, Socket clientSocket, long clientId)
    {
        public long ClientId { get; } = clientId;
        readonly DataTransferServer _server = server;
        readonly Socket _soket = clientSocket;
        NetworkStream? _networkStram;
        BinaryReader? _binaryReader;
        BinaryWriter? _binaryWriter;
        Thread? _thread;
        volatile bool _working = false;

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
                        ReceivedRequest(this, jObject);
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
