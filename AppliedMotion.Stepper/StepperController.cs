using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AppliedMotion.Stepper
{
    public class StepperController : IDisposable
    {
        public static bool messageReceived = false;
        public int MaxStepsPerRev = 51200;
        public int MinStepsPerRev = 200;
        public StepperMotor Sm = new StepperMotor();
        internal static int listenPort = 7775;
        private UdpClient _udpClient;
        private bool _waitingForResponse = false;
        private int sendPort = 7777;

        public StepperController(string ipAddress)
        {
            Sm.IP = IPAddress.Parse(ipAddress);
            _udpClient = new UdpClient(sendPort);
            _udpClient.AllowNatTraversal(true);
            //_udpClient.ExclusiveAddressUse = false;
            _udpClient.Connect(Sm.IP, listenPort);

            ThreadPool.QueueUserWorkItem(delegate { ReceiveMessages(); }, null);
        }

        public static void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = ((UdpState)(ar.AsyncState)).udpState;
            IPEndPoint e = ((UdpState)(ar.AsyncState)).IpEndPoint;

            byte[] receiveBytes = u.EndReceive(ar, ref e);
            string receiveString = Encoding.ASCII.GetString(receiveBytes);

            Console.WriteLine($"Received: {receiveString}");
            messageReceived = true;
        }

        public static void ReceiveMessages()
        {
            // Receive a message and write it to the console.
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, listenPort);
            UdpClient udpState = new UdpClient(endPoint);

            UdpState state = new UdpState();
            state.IpEndPoint = endPoint;
            state.udpState = udpState;

            Console.WriteLine("listening for messages");
            udpState.BeginReceive(new AsyncCallback(ReceiveCallback), state);

            // Do some work while we wait for a message. For this example, we'll just sleep
            while (!messageReceived)
            {
                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            this.Sm.Dispose();
            _udpClient.Close();
            _udpClient?.Dispose();
        }
#region commands

        public void ChangeJogSpeed(double speed)
        {
            SendSclCommand($"CS{System.Math.Round(speed, 2)}");
        }

        public void DisableMotor()
        {
            SendSclCommand("MD");
        }

        public void EnableMotor()
        {
            SendSclCommand("ME");
        }

        public void GetAlarmCode()
        {
            SendSclCommand("AL");
        }

        public void GetEncoderCounts()
        {
            SendSclCommand("IFD");
        }

        public void GetEncoderPosition()
        {
            SendSclCommand("SP");
        }

        public void GetModel()
        {
            SendSclCommand("MV");
        }

        public void GetStatus()
        {
            SendSclCommand("SC");
        }

        public void MoveRelativeSteps(long steps)
        {
            SendSclCommand($"DI{steps}");
            SendSclCommand($"FL");
            WaitForStop();
        }

        public void MoveToAbsolutePosition(long position)
        {
            SendSclCommand($"DI{position}");
            SendSclCommand($"FP");
            WaitForStop();
        }

        public void ResetEncoderPosition(long newValue)
        {
            SendSclCommand($"EP{newValue}");
            SendSclCommand($"SP{newValue}");
        }

        public void SetNumberStepsPerRevolution(int numberSteps)
        {
            // ensure that number is divisible by two
            Math.DivRem(numberSteps, 2, out int EvenNumberSteps);
            EvenNumberSteps = numberSteps + EvenNumberSteps;
            if (EvenNumberSteps <= MaxStepsPerRev && EvenNumberSteps >= MinStepsPerRev)
            {
                SendSclCommand($"EG{EvenNumberSteps}");
                this.Sm.StepsPerRev = numberSteps;
            }
            else if (EvenNumberSteps >= MaxStepsPerRev)
            {
                SendSclCommand($"EG{MaxStepsPerRev}");
                this.Sm.StepsPerRev = MaxStepsPerRev;
            }
            else if (EvenNumberSteps <= MinStepsPerRev)
            {
                SendSclCommand($"EG{MinStepsPerRev}");
                this.Sm.StepsPerRev = MinStepsPerRev;
            }
        }

        public void SetVelocity(double revsPerSec)
        {
            SendSclCommand($"VE{System.Math.Round(revsPerSec, 3)}");
        }

        public void StartJog(double speed, double acceleration, double deceleration)
        {
            SendSclCommand($"JS{Math.Round(speed, 2)}");
            SendSclCommand($"JA{Math.Round(acceleration, 2)}");
            SendSclCommand($"JL{Math.Round(deceleration, 2)}");
            SendSclCommand("JM1");
            SendSclCommand("CJ");
        }

        public void Stop()
        {
            SendSclCommand("ST");
        }

        public void StopJog()
        {
            SendSclCommand("SJ");
        }

#endregion commands

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
                System.Threading.Thread.Sleep(10);
            }

            _waitingForResponse = true;
            SendSclCommand(command);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.Elapsed < timeout)
            {
                var responseText = GetResponse();
                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    Debug.Print($"RX: {responseText} @ {DateTime.Now.ToString("hh:mm:ss")}");
                    return responseText.Trim();
                }
                System.Threading.Thread.Sleep(10);
            }

            return null;
        }

        internal void WaitForStop()
        {
            try
            {
                this.GetStatus();
                while (Sm.MotorStatus.Moving.ToString() != "False")
                {
                    this.GetStatus();
                    this.GetAlarmCode();
                    System.Threading.Thread.Sleep(00);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Debug.Print(e.Message);
            }
        }

        private string GetResponse()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 1000)
            {
                if (_udpClient.Available > 0) { break; }
            }
            if (_udpClient.Available == 0)
            {
                _waitingForResponse = false;
                return null;
            }

            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
            byte[] receiveBytes = _udpClient.Receive(ref remoteIpEndPoint);
            _waitingForResponse = false;
            byte[] sclString = new byte[receiveBytes.Length - 2];

            for (int i = 0; i < sclString.Length; i++)
            {
                sclString[i] = receiveBytes[i + 2];
            }
            return Encoding.ASCII.GetString(sclString);
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
            Debug.Print($"TX: {command}");
        }

        public struct UdpState
        {
            public IPEndPoint IpEndPoint;
            public UdpClient udpState;
        }
    }
}