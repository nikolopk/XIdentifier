using System.Text;
using Abstractions;
using SuperSimpleTcp;
using SystemDiagnostics = System.Diagnostics;

namespace XServer
{
    internal class Server
    {
        //Const Variables
        const string ConnectionString = "127.0.0.1:8005";
        const int ValidationLoops = 20;

        static SimpleTcpServer _server;
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static Random _rnd = new Random(Environment.TickCount);
        static SystemDiagnostics.Stopwatch _stopwatch = new SystemDiagnostics.Stopwatch();

        static bool _validUser = true;
        static string _clientIpPort;
        static int _b, _x, _n, _v;
        static int _validCounter = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("Let's Start...");
            try
            {
                _server = new SimpleTcpServer(ConnectionString);
                _server.Events.ClientConnected += ClientConnected;
                _server.Events.ClientDisconnected += ClientDisconnected;
                _server.Events.DataReceived += DataReceived;
                _server.Start();

                Console.CancelKeyPress += (sender, eArgs) =>
                {
                    _quitEvent.Set();
                    eArgs.Cancel = true;
                };
                _quitEvent.WaitOne();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception {exception}");
            }
        }

        static void ClientConnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}] client connected");

            _stopwatch.Restart();
            _clientIpPort = e.IpPort;
        }

        static void ClientDisconnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}] client disconnected: {e.Reason}");
        }

        static void DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"[{e.IpPort}]: {Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count)}");

            var parsedMessage = MessageHelper.ParseMessage(Encoding.UTF8.GetString(e.Data.Array, 0, e.Data.Count));
            if (parsedMessage.state.Equals(StateInfo.TransmissionState.TransmitPublicKey))
            {
                _n = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.N).Value;
                _v = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.V).Value;

                _validUser = true;
                _validCounter = 0;

                var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.RequestX);
                _server.Send(_clientIpPort, data);
            }
            else if (parsedMessage.state.Equals(StateInfo.TransmissionState.TransmitX))
            {
                _x = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.X).Value;

                CreateB();
            }
            else if (parsedMessage.state.Equals(StateInfo.TransmissionState.TransmitR) && _b == 0)
            {
                Console.WriteLine($"Checking...");
                var r = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.r).Value;
                var tempValue = Math.Pow(r, 2) % _n;

                if (tempValue == _x)
                {
                    Console.WriteLine($"Valid Transaction");
                    _validCounter++;
                }
                else
                {
                    Console.WriteLine("Identification Failed");
                }

                CheckAndRetrigger();
            }
            else if (parsedMessage.state.Equals(StateInfo.TransmissionState.TransmitY) && _b == 1)
            {
                Console.WriteLine($"Checking...");
                var y = parsedMessage.entries.FirstOrDefault(entry => entry.Name == DataEntry.EntryNames.Y).Value;
                var tempValue = (Math.Pow(y, 2) * _v) % _n;

                if (tempValue == _x)
                {
                    Console.WriteLine($"Valid Transaction");
                    _validCounter++;
                }
                else
                {
                    Console.WriteLine("Identification Failed");
                }

                CheckAndRetrigger();
            }
            else
            {
                Console.WriteLine($"Not Valid Operation");
                _validUser = false;
                CheckAndRetrigger();
            }
        }

        static void CreateB()
        {
            lock (_rnd)
            {
                _b = _rnd.Next(0, 2);
            }

            Console.WriteLine($"Sending {_b}");

            var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.TransmitB,
                new List<DataEntry>
                {
                    new DataEntry { Name= DataEntry.EntryNames.b,Value= _b }
                });
            _server.Send(_clientIpPort, data);
        }

        static void CheckAndRetrigger()
        {
            Console.WriteLine("Check for triggering...");

            if (_validUser && _validCounter < ValidationLoops)
            {
                CreateB();
            }
            else if (_validUser && _validCounter == ValidationLoops)
            {
                _stopwatch.Stop();
                Console.WriteLine($"User identified successfully in {_stopwatch.ElapsedMilliseconds} ms!! :)");

                var data = MessageHelper.CreateMessage(StateInfo.TransmissionState.Exit,
                    new List<DataEntry>
                    {
                        new DataEntry { Name= DataEntry.EntryNames.Success, Value= 1 },
                        new DataEntry { Name= DataEntry.EntryNames.ExecutionTime, Value= (int)_stopwatch.ElapsedMilliseconds }
                    });
                _server.Send(_clientIpPort, data);
            }
            else
            {
                Console.WriteLine("Validation Failed :(");
            }
        }
    }
}