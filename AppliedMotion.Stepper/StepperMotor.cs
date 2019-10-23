
using System;
using System.Net;

namespace AppliedMotion.Stepper
{
 
    public class StepperMotor : IDisposable
    {
        #region Properties

        public AlarmCode AlarmCode { get; set; }

        public double EncoderCounts { get; set; }

        public double EncoderPosition { get; set; }

        public IPAddress Ip { get; set; }

        public string Model { get; set; }

        public MotorStatus MotorStatus { get; set; }

        public double StepsPerRev { get; set; }

        #endregion Properties

        #region Methods

        public void Dispose()
        {
        }

        #endregion Methods
    }
}