using System;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppliedMotion.Stepper
{
    [TestClass]
    public class UnitTest1
    {
        private string IP = "10.10.10.10";

        [TestMethod]
        public void MinStepsPerRevCoerced()
        {
            StepperController sc = new StepperController(IP);
            sc.SetNumberStepsPerRevolution(1);
            Assert.AreEqual(sc.MinStepsPerRev ,sc.Sm.StepsPerRev);
            sc.Dispose();

        }

        [TestMethod]
        public void MaxStepsPerRevCoerced()
        {
            StepperController sc = new StepperController(IP);
            sc.SetNumberStepsPerRevolution(10000000);
            Assert.AreEqual(sc.MaxStepsPerRev, sc.Sm.StepsPerRev);
            sc.Dispose();
        }

        [TestMethod]

        public void SetStepsToAcceptableValue()
        {
            StepperController sc = new StepperController(IP);
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
        public void RotateTenRevolutions()
        {
            double revsPerSecond = 10;
            StepperController sc = new StepperController(IP);
            sc.SetVelocity(revsPerSecond);
            sc.ResetEncoderPosition(0);
            sc.EnableMotor();
            sc.SetNumberStepsPerRevolution(sc.MaxStepsPerRev);
            double calculatedPosition = sc.MaxStepsPerRev * 10;
            sc.MoveToAbsolutePosition((long)calculatedPosition);
            Thread.Sleep(2500);
            sc.GetEncoderPosition();
            Assert.AreEqual(calculatedPosition,sc.Sm.EncoderPosition );
            sc.Dispose();
        }
    }
}
