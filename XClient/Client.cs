using System.Text;
using System.Text.Json;
using Abstractions;
using Org.BouncyCastle.Math;
using SuperSimpleTcp;

namespace XClient
{
    internal class Client
    {
        //Const Variables
        const string ConnectionString = "127.0.0.1:8005";

        static SimpleTcpClient _client;
        static BigInteger _n, _s, _v;
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static Random _rnd = new Random(Environment.TickCount);

        static int _r, _x, _y;

        static void Main(string[] args)
        {
            Random rnd = new Random();
            Console.WriteLine("Hello, Client!");

            try
            {
                _client = new SimpleTcpClient(ConnectionString);
                _client.Events.Connected += Connected;
                _client.Events.Disconnected += Disconnected;
                _client.Events.DataReceived += DataReceived;
                _client.ConnectWithRetries(10000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };
            _quitEvent.WaitOne();
        }

        static void Connected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"*** Server {e.IpPort} connected");

            TriggerFFSIS();
            CalculateXAndY();
        }

        static void Disconnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"*** Server {e.IpPort} disconnected");
        }

        static void DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}] {Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count)}");

            var parsedMessage = MessageHelper.ParseMessage(Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count));

            if (parsedMessage.state.Equals(StateInfo.TransmissionState.RequestX))
            {
                var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.TransmitX,
                    new List<DataEntry>
                    {
                        new DataEntry { Name = DataEntry.EntryNames.X, Value = _x }
                    });
                _client.Send(data);
            }
            else if (parsedMessage.state.Equals(StateInfo.TransmissionState.TransmitB))
            {
                var b = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.b).Value;
                if (b == 0)
                {
                    Console.WriteLine("b = 0");

                    var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.TransmitR,
                        new List<DataEntry>
                        {
                            new DataEntry { Name = DataEntry.EntryNames.r, Value = _r }
                        });
                    _client.Send(data);
                }
                else
                {
                    Console.WriteLine("b = 1");

                    var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.TransmitY,
                        new List<DataEntry>
                        {
                            new DataEntry { Name = DataEntry.EntryNames.Y, Value = _y }
                        });
                    _client.Send(data);
                }
            }
            else if (parsedMessage.state.Equals(StateInfo.TransmissionState.Exit))
            {
                var success = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.Success).Value == 1 ? true : false;

                if (success)
                {
                    var executionTimeInMs = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.ExecutionTime).Value;
                    
                    Console.WriteLine($"Success validation! Execution Time was {executionTimeInMs} ms!!");
                    Environment.Exit(executionTimeInMs);
                }
                else
                {
                    Console.WriteLine("Validation Failed :(");
                    Environment.Exit(-1);
                }
            }
        }

        static void TriggerFFSIS()
        {
            Console.WriteLine("Starting FFSIS...");

            BigInteger v = new BigInteger("58");
            _n = new BigInteger("77");
            _s = new BigInteger("2");
            _v = v.ModInverse(_n);

            var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.TransmitPublicKey,
                new List<DataEntry>
                {
                    new DataEntry { Name = DataEntry.EntryNames.N, Value = _n.IntValue },
                    new DataEntry { Name = DataEntry.EntryNames.V, Value = v.IntValue }
                });
            _client.Send(data);
        }

        static void CalculateXAndY()
        {
            lock (_rnd)
            {
                _r = _rnd.Next(1, _n.IntValue);
            }

            _x = (int)Math.Pow(_r, 2) % _n.IntValue;
            _y = (_r * _s.IntValue) % _n.IntValue;
        }
    }
}