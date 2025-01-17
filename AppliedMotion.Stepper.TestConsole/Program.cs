﻿using log4net.Config;
using System;
using System.IO;
using System.Threading;

namespace AppliedMotion.Stepper.TestConsole
{
    internal class Program
    {
        #region Methods

        private static void Main()
        {
            // Configure Log4Net
            XmlConfigurator.Configure(new FileInfo("log4net.config"));

            // Configure PostSharp Logging to use Log4Net

            StepperController sc = new StepperController("10.10.10.10");
            try
            {
                // stop the drive from moving

                sc.Stop();
                sc.EnableMotor();
                sc.ClearAlarms();
                sc.GetModel();
                sc.GetStatus();
                sc.StartListening();
                sc.SetFormatDecimal();

                Thread.Sleep(500);

                Thread.Sleep(2000);

                Console.WriteLine("Resetting position to 0");
                sc.ResetEncoderPosition(0);

                //Console.WriteLine("Moving...");

                //System.Threading.Thread.Sleep(2000);
                //sc.StartJog(-0.5, 25, 25);
                //System.Threading.Thread.Sleep(2500);
                //sc.ChangeJogSpeed(2.5);
                //System.Threading.Thread.Sleep(2500);
                //sc.ChangeJogSpeed(.5);
                //System.Threading.Thread.Sleep(2500);
                //sc.StopJog();
                //Console.WriteLine("Move complete.");

                //Console.WriteLine("Status: " + sc.GetStatus());
                Thread.Sleep(1000);

                //Console.WriteLine("Current Position: " + sc.GetEncoderPosition());

                // stop the drive from moving
                sc.Stop();
                sc.ClearAlarms();

                // set the number of steps per rev
                sc.SetNumberStepsPerRevolution(51200);

                // set revolutions per second
                sc.SetVelocity(25);

                sc.GetModel();
                Console.WriteLine($"Model: {sc.Sm.Model}");

                sc.GetStatus();
                sc.GetEncoderPosition();

                Console.WriteLine("Resetting position to 0");
                sc.ResetEncoderPosition(0);
                sc.EnableMotor();

                // read 15 positions
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("read 15 positions. 1 revolution = 51200 steps " +
                                  "\nmin number: -2147483648" +
                                  "\nmax number: 2147483647");
                int maxPositions = 15;
                for (int i = 0; i < maxPositions; i++)
                {
                    try
                    {
                        Console.WriteLine($"Enter a position #{i} of {maxPositions}");

                        long position = (long)Convert.ToDouble(Console.ReadLine());
                        sc.MoveToAbsolutePosition(position);
                        sc.GetEncoderPosition();
                        Console.WriteLine($"Current Position: {sc.Sm.EncoderPosition}");
                    }
                    catch (Exception e)
                    {
                        StepperController.Log.Error(e.Message);
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                StepperController.Log.Error(e.Message);
                Console.WriteLine(e);
            }
            finally
            {
                sc.Stop();
                sc.DisableMotor();
                sc.StopListening();
            }
        }

        #endregion Methods
    }
}