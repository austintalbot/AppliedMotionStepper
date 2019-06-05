using System;
using System.Net;

namespace AppliedMotion.Stepper
{
    public class StepperMotor : IDisposable
    {
        public void Dispose()
        {
        }

        public double EncoderPosition { get; set; }
        public IPAddress IP { get; set; }
        public MotorStatus MotorStatus { get; set; }

        public string Model { get; set; }
        public double StepsPerRev { get; set; }
    }
}