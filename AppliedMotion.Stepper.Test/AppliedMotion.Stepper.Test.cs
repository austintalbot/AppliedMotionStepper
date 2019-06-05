using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace AppliedMotion.Stepper
{
    [TestClass]
    public class UnitTest1
    {
        #region Fields

        private string IP = "10.10.10.10";

        #endregion Fields

        #region Methods

        [TestMethod]
        public void GetAlarmCodes()
        {
            StepperController sc = new StepperController(IP);
            sc.startListening();
            BitArray bA = new BitArray(System.BitConverter.GetBytes(0));
            AlarmCode blankAlarmCode = new AlarmCode(bA);

            sc.GetAlarmCode();
            Thread.Sleep(500);
            AlarmCode alarmCode = sc.Sm.AlarmCode;
            Assert.IsNotNull(sc.Sm.AlarmCode);

            sc.Sm.AlarmCode.CwLimit = true;
            Assert.AreNotEqual(blankAlarmCode.CwLimit, sc.Sm.AlarmCode.CwLimit);
            sc.ClearAlarms();
            sc.GetAlarmCode();

            Thread.Sleep(500);

            Assert.AreEqual(blankAlarmCode.CwLimit, sc.Sm.AlarmCode.CwLimit);

            sc.Dispose();
        }

        [TestMethod]
        public void GetEncoderCounts()
        {
            StepperController sc = new StepperController(IP);
            sc.EnableMotor();
            sc.startListening();
            sc.GetEncoderCounts();
            Thread.Sleep(1000);
            Debug.Print($"EncoderCounts = {sc.Sm.encoderCounts}");
            double counts = sc.Sm.encoderCounts;

            sc.MoveToAbsolutePosition(51200);
            sc.GetEncoderCounts();
            Thread.Sleep(1000);
            double newCounts = sc.Sm.encoderCounts;

            Debug.Print($"EncoderCounts = {sc.Sm.encoderCounts}");
            Thread.Sleep(1000);
            Assert.AreEqual(sc.Sm.encoderCounts, newCounts);
            sc.Dispose();
        }

        [TestMethod]
        public void MaxStepsPerRevCoerced()
        {
            StepperController sc = new StepperController(IP);
            sc.startListening();
            sc.SetNumberStepsPerRevolution(10000000);
            Assert.AreEqual(sc.MaxStepsPerRev, sc.Sm.StepsPerRev);
            sc.Dispose();
        }

        [TestMethod]
        public void MinStepsPerRevCoerced()
        {
            StepperController sc = new StepperController(IP);
            sc.startListening();
            sc.SetNumberStepsPerRevolution(1);
            Assert.AreEqual(sc.MinStepsPerRev, sc.Sm.StepsPerRev);
            sc.Dispose();
        }

        [TestMethod]
        public void RotateTenRevolutions()
        {
            double revsPerSecond = 5;
            StepperController sc = new StepperController(IP);
            sc.startListening();
            sc.SetVelocity(revsPerSecond);
            sc.ResetEncoderPosition(0);
            sc.EnableMotor();
            sc.SetNumberStepsPerRevolution(sc.MaxStepsPerRev);
            double calculatedPosition = sc.MaxStepsPerRev * 10;
            sc.MoveToAbsolutePosition((long)calculatedPosition);
            Thread.Sleep(2500);
            sc.GetEncoderPosition();
            Thread.Sleep(500);
            Debug.Print($"Calculated ={calculatedPosition}");
            Debug.Print($"Encoder Position = {sc.Sm.EncoderPosition}");
            Assert.AreEqual(calculatedPosition, sc.Sm.EncoderPosition);
            sc.Dispose();
        }

        [TestMethod]
        public void SetStepsToAcceptableValue()
        {
            StepperController sc = new StepperController(IP);
            sc.startListening();
            int expectedStepsPerRev = 20000;

            sc.SetNumberStepsPerRevolution(expectedStepsPerRev);
            Assert.AreEqual(expectedStepsPerRev, sc.Sm.StepsPerRev);

            expectedStepsPerRev = 10000;
            sc.SetNumberStepsPerRevolution(expectedStepsPerRev);
            Assert.AreEqual(expectedStepsPerRev, sc.Sm.StepsPerRev);

            expectedStepsPerRev = 200;
            sc.SetNumberStepsPerRevolution(expectedStepsPerRev);
            Assert.AreEqual(expectedStepsPerRev, sc.Sm.StepsPerRev);

            expectedStepsPerRev = 51101;
            sc.SetNumberStepsPerRevolution(expectedStepsPerRev);
            Assert.AreEqual(expectedStepsPerRev, sc.Sm.StepsPerRev);

            expectedStepsPerRev = 51099;
            sc.SetNumberStepsPerRevolution(expectedStepsPerRev);
            Assert.AreEqual(expectedStepsPerRev, sc.Sm.StepsPerRev);

            expectedStepsPerRev = 205;
            sc.SetNumberStepsPerRevolution(expectedStepsPerRev);
            Assert.AreEqual(expectedStepsPerRev, sc.Sm.StepsPerRev);
            sc.Dispose();
        }

        #endregion Methods
    }
}