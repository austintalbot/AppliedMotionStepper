using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace AppliedMotion.Stepper.Test
{
    [TestClass]
    public class AppliedMotionStepperTest
    {
        #region Fields

        private string IP = "10.10.10.10";

        #endregion Fields

        #region Methods

        [TestMethod]
        public void GetAlarmCodes()
        {
            StepperController sc = new StepperController(IP);
            sc.StartListening();
            BitArray bA = new BitArray(BitConverter.GetBytes(0));
            AlarmCode blankAlarmCode = new AlarmCode(bA);

            sc.GetAlarmCode();
            Thread.Sleep(500);

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

            //sc.ResetEncoderPosition(0);

            sc.SetFormatDecimal();
            sc.StartListening();
            sc.GetEncoderCounts();
            sc.GetEncoderPosition();
            Thread.Sleep(100);
            Debug.Print($"Encoder position= {sc.Sm.EncoderPosition}");
            Debug.Print($"Encoder counts= {sc.Sm.EncoderCounts}");

            sc.StartJog(5, 5, 5);
            Thread.Sleep(1000);
            sc.StopJog();
            sc.GetEncoderPosition();
            sc.GetEncoderCounts();
            Thread.Sleep(2000);
            double newCounts = sc.Sm.EncoderCounts;
            double newPosition = sc.Sm.EncoderPosition;
            Debug.Print($"Encoder position= {sc.Sm.EncoderPosition}");
            Debug.Print($"Encoder counts= {sc.Sm.EncoderCounts}");
            Thread.Sleep(1000);
            Assert.AreEqual(sc.Sm.EncoderPosition, newPosition);
            Assert.AreEqual(sc.Sm.EncoderCounts, newCounts);
            Thread.Sleep(1000);
            sc.StopListening();

            sc.Dispose();
        }

        [TestMethod]
        public void MaxStepsPerRevCoerced()
        {
            StepperController sc = new StepperController(IP);
            sc.StartListening();
            sc.SetNumberStepsPerRevolution(10000000);
            Assert.AreEqual(sc.MaxStepsPerRev, sc.Sm.StepsPerRev);
            sc.Dispose();
        }

        [TestMethod]
        public void MinStepsPerRevCoerced()
        {
            StepperController sc = new StepperController(IP);
            sc.StartListening();
            sc.SetNumberStepsPerRevolution(1);
            Assert.AreEqual(sc.MinStepsPerRev, sc.Sm.StepsPerRev);
            sc.Dispose();
        }

        [TestMethod]
        public void RotateTenRevolutions()
        {
            double revsPerSecond = 5;
            StepperController sc = new StepperController(IP);
            sc.StartListening();
            sc.SetFormatDecimal();
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
            sc.StopListening();
            sc.Dispose();
        }

        [TestMethod]
        public void SetStepsToAcceptableValue()
        {
            StepperController sc = new StepperController(IP);
            sc.StartListening();
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

        [TestMethod]
        public void TestCcwLimit()
        {
            Random random = new Random();
            double revsPerSecond = 2;
            StepperController sc = new StepperController(IP);
            sc.StartListening();
            sc.SetFormatDecimal();
            sc.SetVelocity(5);
            sc.ResetEncoderPosition(0);

            double numberTurns = random.NextDouble() * 2.5;
            double ccwLimitCounts = -1 * Math.Floor(sc.MaxStepsPerRev * numberTurns);
            sc.SetCwLimit(ccwLimitCounts);

            sc.SetVelocity(revsPerSecond);

            sc.ResetEncoderPosition(0);
            sc.EnableMotor();
            sc.SetNumberStepsPerRevolution(sc.MaxStepsPerRev);

            // move to number of turns + ten percent
            double calculatedPosition = Math.Floor((ccwLimitCounts * 2));
            sc.MoveToAbsolutePosition((long)calculatedPosition);

            Thread.Sleep(2500);

            sc.GetEncoderPosition();

            Thread.Sleep(500);
            Debug.Print($"Limit ={ccwLimitCounts}");
            Debug.Print($"Calculated ={calculatedPosition}");
            Debug.Print($"Encoder Position = {sc.Sm.EncoderPosition}");

            // over position is limited by software limit
            Assert.AreNotEqual(calculatedPosition, sc.Sm.EncoderPosition);

            // software limit and encoder position match
            Assert.AreEqual(ccwLimitCounts, sc.Sm.EncoderPosition);

            // remove software limit
            sc.ClearCwLimit();
            sc.MoveToAbsolutePosition((long)calculatedPosition);

            // over position is not limited by software limit and matches the encoder position
            Assert.AreEqual(calculatedPosition, sc.Sm.EncoderPosition);
            sc.Dispose();
        }

        [TestMethod]
        public void TestCwLimit()
        {
            Random random = new Random();
            double revsPerSecond = 2;
            StepperController sc = new StepperController(IP);
            sc.StartListening();
            sc.SetFormatDecimal();
            sc.SetVelocity(5);
            sc.ResetEncoderPosition(0);

            double numberTurns = random.NextDouble() * 2.5;
            double cwLimitCounts = Math.Floor(sc.MaxStepsPerRev * numberTurns);
            sc.SetCwLimit(cwLimitCounts);

            sc.SetVelocity(revsPerSecond);

            sc.ResetEncoderPosition(0);
            sc.EnableMotor();
            sc.SetNumberStepsPerRevolution(sc.MaxStepsPerRev);

            // move to number of turns + ten percent
            double calculatedPosition = Math.Floor((cwLimitCounts * 1.1));
            sc.MoveToAbsolutePosition((long)calculatedPosition);

            Thread.Sleep(2500);

            sc.GetEncoderPosition();

            Thread.Sleep(500);
            Debug.Print($"Limit ={cwLimitCounts}");
            Debug.Print($"Calculated ={calculatedPosition}");
            Debug.Print($"Encoder Position = {sc.Sm.EncoderPosition}");

            // over position is limited by software limit
            Assert.AreNotEqual(calculatedPosition, sc.Sm.EncoderPosition);

            // software limit and encoder position match
            Assert.AreEqual(cwLimitCounts, sc.Sm.EncoderPosition);

            // remove software limit
            sc.ClearCwLimit();
            sc.MoveToAbsolutePosition((long)calculatedPosition);

            // over position is not limited by software limit and matches the encoder position
            Assert.AreEqual(calculatedPosition, sc.Sm.EncoderPosition);
            sc.Dispose();
        }

        #endregion Methods
    }
}