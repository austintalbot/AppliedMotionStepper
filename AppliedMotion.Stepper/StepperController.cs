using log4net;
using PostSharp.Patterns.Model;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppliedMotion.Stepper
{
    [NotifyPropertyChanged]
    public partial class StepperController : IDisposable
    {
        #region Fields

        public static readonly ILog Log = LogManager.GetLogger(typeof(StepperController));

        public static bool MessageReceived = false;

        public int MaxStepsPerRev = 51200;

        public int MinStepsPerRev = 200;

        public StepperMotor Sm = new StepperMotor();

        internal static int ListenPort = 7775;

        private const int SendPort = 7777;

        private readonly CancellationTokenSource _source;

        private readonly UdpClient _udpClient;

        private CancellationToken _cancellationToken;

        private bool _waitingForResponse;

        #endregion Fields

        #region Constructors

        public StepperController(string ipAddress)
        {
            _source = new CancellationTokenSource();
            _cancellationToken = _source.Token;
            IPAddress.TryParse(ipAddress, out IPAddress ip);
            Sm.Ip = ip;
            _udpClient = new UdpClient(SendPort);
            _udpClient.AllowNatTraversal(true);

            //_udpClient.ExclusiveAddressUse = false;
            _udpClient.Connect(Sm.Ip, ListenPort);
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            Log.Info("stepper controller being disposed");
            _udpClient.Close();

            //_udpClient?.Dispose();
        }

        public string SendSclCommandAndGetResponse(string command)
        {
            Log.Info($"command sent: {command}");
            return SendSclCommandAndGetResponse(command, TimeSpan.FromSeconds(1));
        }

        public string SendSclCommandAndGetResponse(string command, TimeSpan timeout)
        {
            Log.Info($"command sent: {command} with timeout: {timeout}");
            int responseTimeout = 5000;
            Stopwatch swConflictTimeout = new Stopwatch();
            swConflictTimeout.Start();
            while (_waitingForResponse && swConflictTimeout.ElapsedMilliseconds < responseTimeout)
            {
                Thread.Sleep(10);
            }

            _waitingForResponse = true;
            SendSclCommand(command);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed < timeout)
            {
                string responseText = GetResponse();
                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    Debug.Print($"RX: {responseText} @ {DateTime.Now:hh:mm:ss}");
                    Log.Info($"RX: {responseText} @ {DateTime.Now:hh:mm:ss}");
                    return responseText.Trim();
                }

                Thread.Sleep(10);
            }

            return null;
        }

        public void StartListening()
        {
            Log.Info($"listening for UDP responses");
            try
            {
                Task.Run(() => _udpClient.BeginReceive(ReceiveCallBack, null), _source.Token);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                Console.WriteLine(e.ToString());
            }
        }

        public void StopListening()
        {
            Log.Info($"Stopped Listening");
            _source.Cancel();
        }

        private string GetResponse()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 1000)
            {
                if (_udpClient.Available > 0)
                {
                    break;
                }
            }

            if (_udpClient.Available == 0)
            {
                _waitingForResponse = false;
                return null;
            }

            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, ListenPort);
            byte[] receiveBytes = _udpClient.Receive(ref remoteIpEndPoint);
            _waitingForResponse = false;

            //create a byte array that is two bytes shorter

            byte[] sclArraycopy = new byte[receiveBytes.Length - 2];
            Array.Copy(receiveBytes, 2, sclArraycopy, 0, receiveBytes.Length - 2);

            string results = Encoding.ASCII.GetString(sclArraycopy);
            Log.Info($"get Response result: {results }");
            return results;
        }

        //CallBack
        private void ReceiveCallBack(IAsyncResult res)
        {
            const string motorStatusResponse = "SC=";
            const string alarmResponse = "AL=";
            const string positionResponse = "IP=";
            const string encoderCountsResponse = "IE=";
            const string encoderPositionResponse = "EP=";
            const string modelResponse = "MV";
            bool defaultReceived = false;
            byte[] received;
            try
            {
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, ListenPort);
                try
                {
                    received = _udpClient.EndReceive(res, ref remoteIpEndPoint);

                    if (received.Length == 4 && received[0] == 0 && received[1] == 7 && received[2] == 37 && received[3] == 13)
                    {
                        defaultReceived = true;
                    }

                    //Process codes
                    string response = Encoding.UTF8.GetString(received);
                    int last = response.LastIndexOf("\r", StringComparison.Ordinal);
                    response = response.Substring(0, last - 0);

                    Log.Debug($"Response: {response}");
                    if (response.Contains(encoderCountsResponse)) // returned encoder position
                    {
                        int first = response.IndexOf(encoderCountsResponse, StringComparison.Ordinal) +
                                    encoderCountsResponse.Length;
                        last = response.Length;
                        string extractedString = response.Substring(first, last - first);

                        double encoderCounts = double.Parse(extractedString);
                        Log.Info($"Encoder Counts: {encoderCounts}");
                        Sm.EncoderCounts = encoderCounts;
                    }
                    else if (response.Contains(alarmResponse)) // returned alarm code
                    {
                        int first = response.IndexOf(alarmResponse, StringComparison.Ordinal) + alarmResponse.Length;
                        last = response.Length;
                        string extractedString = response.Substring(first, last - first);
                        int responseCode = Convert.ToInt32(extractedString);
                        BitArray bA = new BitArray(BitConverter.GetBytes(responseCode));
                        Sm.AlarmCode = new AlarmCode(bA);
                        Log.Info($"Alarm Code: {Sm.AlarmCode}");
                    }
                    else if (response.Contains(positionResponse))
                    {
                        int first = response.IndexOf(positionResponse, StringComparison.Ordinal) +
                                    positionResponse.Length;
                        last = response.Length;
                        string extractedString = response.Substring(first, last - first);
                        long encoderPosition = long.Parse(extractedString);
                        Sm.EncoderPosition = encoderPosition;
                        Log.Info($"Encoder Position: {Sm.EncoderPosition}");
                    }
                    else if (response.Contains(motorStatusResponse))
                    {
                        int first = response.IndexOf(motorStatusResponse, StringComparison.Ordinal) +
                                    motorStatusResponse.Length;
                        last = response.Length;
                        string extractedString = response.Substring(first, last - first);
                        int responseCode = Convert.ToInt32(extractedString);
                        BitArray bA = new BitArray(BitConverter.GetBytes(responseCode));
                        Sm.MotorStatus = new MotorStatus(bA);
                        Log.Info($"Motor Status: {Sm.MotorStatus}");
                    }
                    else if (response.Contains(encoderPositionResponse))
                    {
                        int first = response.IndexOf(encoderPositionResponse, StringComparison.Ordinal) +
                                    encoderPositionResponse.Length;
                        last = response.Length;
                        string extractedString = response.Substring(first, last - first);

                        Sm.EncoderCounts = Convert.ToDouble(extractedString);
                        Log.Info($"Encoder Position: {Sm.EncoderCounts}");
                    }
                    else if (response.Contains(modelResponse))
                    {
                        int first = response.IndexOf(encoderPositionResponse, StringComparison.Ordinal) +
                                    modelResponse.Length;
                        last = response.Length;
                        string extractedString = response.Substring(first, last - first);
                        Sm.Model = extractedString;
                    }
                    else if (defaultReceived)
                    {
                        // Log.Info($"response : {response} received.");
                        Debug.Print(response);
                        defaultReceived = false;
                    }
                    else
                    {
                        Log.Error($"Unhandled Response: {response} received.");
                        Debug.Print(response);
                    }

                    _udpClient.BeginReceive(ReceiveCallBack, null);
                }
                catch (Exception e)
                {
                    _udpClient.BeginReceive(ReceiveCallBack, null);
                    Log.Error(e.Message);
                }
            }
            catch (Exception e)
            {
                Log.Error($"error: {e.Message} received.");
                Debug.Print(e.Message);
            }
        }

        private void SendSclCommand(string command)
        {
            byte[] sclString = Encoding.ASCII.GetBytes(command);
            byte[] sendBytes = new byte[sclString.Length + 3];
            sendBytes[0] = 0;
            sendBytes[1] = 7;
            Array.Copy(sclString, 0, sendBytes, 2, sclString.Length);
            sendBytes[sendBytes.Length - 1] = 13;
            _udpClient.Send(sendBytes, sendBytes.Length);
            Debug.Print($"TX: {command} @ {DateTime.Now}");
            Log.Info($"TX: { command} @ { DateTime.Now}");
        }

        #endregion Methods
    }
}