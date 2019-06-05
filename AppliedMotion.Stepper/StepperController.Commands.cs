using System;
using System.Diagnostics;
using System.Threading;

namespace AppliedMotion.Stepper
{
    public partial class StepperController
    {
        #region Methods

        public void ChangeJogSpeed(double speed)
        {
            SendSclCommandAndGetResponse($"CS{System.Math.Round(speed, 2)}");
        }

        public void ClearAlarms()
        {
            SendSclCommand("AR");
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
            SendSclCommand("IE");
        }

        public void GetEncoderPosition()
        {
            SendSclCommand("SP");
        }

        public void GetModel()
        {
            SendSclCommandAndGetResponse("MV");
        }

        public void GetStatus()
        {
            SendSclCommandAndGetResponse("SC");
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
                Sm.StepsPerRev = numberSteps;
            }
            else if (EvenNumberSteps >= MaxStepsPerRev)
            {
                SendSclCommand($"EG{MaxStepsPerRev}");
                Sm.StepsPerRev = MaxStepsPerRev;
            }
            else if (EvenNumberSteps <= MinStepsPerRev)
            {
                SendSclCommand($"EG{MinStepsPerRev}");
                Sm.StepsPerRev = MinStepsPerRev;
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
            Thread.Sleep(50);
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

        internal void WaitForStop()
        {
            try
            {
                GetStatus();
                while (Sm.MotorStatus.Moving.ToString() != "False")
                {
                    GetStatus();
                    GetAlarmCode();
                    System.Threading.Thread.Sleep(00);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Debug.Print(e.Message);
            }
        }

        #endregion Methods
    }
}