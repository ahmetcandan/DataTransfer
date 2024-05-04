using System.Net.Sockets;
using System.Text;

namespace DataTransfer.Server;

public class DataTransferConnection(int Port)
{
    private TcpListener _tcpListener;
    private volatile bool _working = false;
    private volatile Thread _thread;
    private readonly object objSenk = new();
    private long _lastClientId = 0;
    private SortedList<long, Client> Clients { get; set; }

    public bool Start()
    {
        if (Connect())
        {
            _working = true;
            _thread = new Thread(new ThreadStart(TListen));
            _thread.Start();
            Console.WriteLine("Server start successfull");
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
            _thread.Join();
            Console.WriteLine("Server stop successfull");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Stop exeption!");
            Console.WriteLine(@$"{ex.Message}
                {ex.InnerException?.Message}
                {ex.StackTrace}");
            return false;
        }
    }

    private bool Connect()

    {
        try
        {
            _tcpListener = new TcpListener(System.Net.IPAddress.Any, Port);
            _tcpListener.Start();
            Console.WriteLine("Server connected successfull");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connection exeption!");
            Console.WriteLine(@$"{ex.Message}
                {ex.InnerException?.Message}
                {ex.StackTrace}");
            return false;
        }
    }

    private bool Disconnect()
    {
        try
        {
            _tcpListener.Stop();
            _tcpListener = null;
            Console.WriteLine("Server disconnected successfull");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Disconnection exeption!");
            Console.WriteLine(@$"{ex.Message}
                {ex.InnerException?.Message}
                {ex.StackTrace}");
            return false;
        }
    }

    private void NewClientConnected(Socket clientSocket)
    {
        Client client;
        lock (objSenk)
        {
            client = new Client(this, clientSocket, ++_lastClientId);
            Clients.Add(client.Id, client);
            Console.WriteLine($"New client added Id: {client.Id}");
        }
        client.Start();
    }

    private void TListen()
    {
        Socket clientSocket;
        while (_working)
        {
            try
            {
                clientSocket = _tcpListener.AcceptSocket();
                if (clientSocket.Connected)
                {
                    try { NewClientConnected(clientSocket); }
                    catch (Exception ex)
                    {

                        Console.WriteLine("ClientSocket exeption!");
                        Console.WriteLine(@$"{ex.Message}
                            {ex.InnerException?.Message}
                            {ex.StackTrace}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TListen exeption!");
                Console.WriteLine(@$"{ex.Message}
                    {ex.InnerException?.Message}
                    {ex.StackTrace}");
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

    private void ClientConnectionClosed(Client client)
    {
        if (_working)
            lock (objSenk)
                if (Clients.ContainsKey(client.Id))
                    Clients.Remove(client.Id);
    }

    public bool HasConnection
    { get { return _working; } }

    private class Client(DataTransferConnection Connection, Socket ClientSocket, long ClientId)
    {
        public long Id { get { return ClientId; } }
        private NetworkStream _networkStram;
        private BinaryReader _binaryReader;
        private BinaryWriter _binaryWriter;
        private volatile bool working = false;
        private Thread thread;

        public bool Start()
        {
            try
            {
                _networkStram = new NetworkStream(ClientSocket);
                _binaryReader = new BinaryReader(_networkStram, Encoding.BigEndianUnicode);
                _binaryWriter = new BinaryWriter(_networkStram, Encoding.BigEndianUnicode);
                thread = new Thread(new ThreadStart(TRun));
                working = true;
                thread.Start();
                Console.WriteLine("Client started");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client.Start exeption!");
                Console.WriteLine(@$"{ex.Message}
                    {ex.InnerException?.Message}
                    {ex.StackTrace}");
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                working = false;
                ClientSocket.Close();
                thread.Join();
                Console.WriteLine("Client stoped");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client.Stop exeption!");
                Console.WriteLine(@$"{ex.Message}
                    {ex.InnerException?.Message}
                    {ex.StackTrace}");
            }
        }

        public void CloseConnection()
        {
            Stop();
        }


        private void TRun()
        {
            while (working)
            {
                try
                {
                    try
                    {
                        string message = _binaryReader.ReadString();
                        Console.WriteLine($"Received message: {message}");
                        ReceivedMessage(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Client.TRun Read Message exeption!");
                        Console.WriteLine(@$"{ex.Message}
                        {ex.InnerException?.Message}
                        {ex.StackTrace}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Client.TRun exeption!");
                    Console.WriteLine(@$"{ex.Message}
                    {ex.InnerException?.Message}
                    {ex.StackTrace}");
                    break;
                }
            }
            working = false;
            try
            {
                if (ClientSocket.Connected)
                {
                    ClientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client.TRun Close Connection exeption!");
                Console.WriteLine(@$"{ex.Message}
                    {ex.InnerException?.Message}
                    {ex.StackTrace}");
            }
            Connection.ClientConnectionClosed(this);
        }

        private void ReceivedMessage(string message)
        {

        }

        public bool SendMessage(string message)
        {
            try
            {
                _binaryWriter.Write(message);
                _networkStram.Flush();
                Console.WriteLine($"Client.SendMessage: {message}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client.SendMessage exeption!");
                Console.WriteLine(@$"{ex.Message}
                    {ex.InnerException?.Message}
                    {ex.StackTrace}");
                return false;
            }
        }
    }
}
