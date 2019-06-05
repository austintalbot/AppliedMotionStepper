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
    public partial class StepperController : IDisposable
    {
        #region Fields

        public static bool MessageReceived = false;
        public int MaxStepsPerRev = 51200;
        public int MinStepsPerRev = 200;
        public StepperMotor Sm = new StepperMotor();
        internal static int ListenPort = 7775;
        private const int SendPort = 7777;
        private readonly UdpClient _udpClient;
        private bool _waitingForResponse = false;

        #endregion Fields

        #region Constructors

        public StepperController(string ipAddress)
        {
            IPAddress.TryParse(ipAddress, out IPAddress Ip);
            Sm.Ip = Ip;
            _udpClient = new UdpClient(SendPort);
            _udpClient.AllowNatTraversal(true);
            //_udpClient.ExclusiveAddressUse = false;
            _udpClient.Connect(Sm.Ip, ListenPort);
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            Sm.Dispose();
            _udpClient.Close();
            _udpClient?.Dispose();
        }

        public string SendSclCommandAndGetResponse(string command)
        {
            return SendSclCommandAndGetResponse(command, TimeSpan.FromSeconds(1));
        }

        public string SendSclCommandAndGetResponse(string command, TimeSpan timeout)
        {
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
                    return responseText.Trim();
                }

                System.Threading.Thread.Sleep(10);
            }

            return null;
        }

        public void startListening()
        {
            try
            {
                Task.Run(() => _udpClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
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
            byte[] sclString = new byte[receiveBytes.Length - 2];

            for (int i = 0; i < sclString.Length; i++)
            {
                sclString[i] = receiveBytes[i + 2];
            }

            return Encoding.ASCII.GetString(sclString);
        }

        //CallBack
        private void ReceiveCallBack(IAsyncResult res)
        {
            const string motorStatusResponse = "SC=";
            const string alarmResponse = "AL=";
            const string positionResponse = "SP=";
            const string encoderCountsResponse = "IE=";
            const string modelResponse = "MV";

            try
            {
                IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, ListenPort);
                byte[] received = _udpClient.EndReceive(res, ref remoteIpEndPoint);

                //Process codes
                string response = Encoding.UTF8.GetString(received);
                if (response.Contains(encoderCountsResponse)) // returned encoder position
                {
                    int first = response.IndexOf(encoderCountsResponse) + encoderCountsResponse.Length;
                    int last = response.LastIndexOf("\r");
                    string ExtractedString = response.Substring(first, last - first);

                    double encoderCounts = double.Parse(ExtractedString);
                    Sm.encoderCounts = encoderCounts;
                }
                else if (response.Contains(alarmResponse)) // returned alarm code
                {
                    int first = response.IndexOf(alarmResponse) + alarmResponse.Length;
                    int last = response.LastIndexOf("\r");
                    string ExtractedString = response.Substring(first, last - first);
                    int responseCode = Convert.ToInt32(ExtractedString);
                    BitArray bA = new BitArray(System.BitConverter.GetBytes(responseCode));
                    Sm.AlarmCode = new Stepper.AlarmCode(bA);
                }
                else if (response.Contains(positionResponse))
                {
                    int first = response.IndexOf(positionResponse) + positionResponse.Length;
                    int last = response.LastIndexOf("\r");
                    string ExtractedString = response.Substring(first, last - first);
                    long encoderPosition = long.Parse(ExtractedString);
                    Sm.EncoderPosition = encoderPosition;
                }
                else if (response.Contains(motorStatusResponse))
                {
                    int first = response.IndexOf(motorStatusResponse) + motorStatusResponse.Length;
                    int last = response.LastIndexOf("\r");
                    string ExtractedString = response.Substring(first, last - first);
                    int responseCode = Convert.ToInt32(ExtractedString);
                    BitArray bA = new BitArray(System.BitConverter.GetBytes(responseCode));
                    Sm.MotorStatus = new Stepper.MotorStatus(bA);
                }
                else
                {
                    Debug.Print(response);
                }

                _udpClient.BeginReceive(new AsyncCallback(ReceiveCallBack), null);
            }
            catch (Exception e)
            {
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
        }

        #endregion Methods
    }
}