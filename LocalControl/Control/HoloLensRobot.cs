/*
    Copyright(c) Microsoft Corp. All rights reserved.
    
    The MIT License(MIT)
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Control
{
    class HoloLensRobot
    {
        public const float wheelDiameter = 16.8f; // CM
        public const float wheelCircumference = (float)(Math.PI * wheelDiameter); // CM
        public const byte microStep = 1;
        public const short stepsPerRotation = (200 * microStep);
        public const float downGear = 18.0f/90.0f;
        public const float stepsPerCM = stepsPerRotation / wheelCircumference / downGear;
        public const float wheelBase = 38.1f; // CM
        public const float wheelBaseRadius = wheelBase / 2.0f;
        public const short maxSpeed = 1500;
        public const short acceleration = 800;
        public const int neckTravelDuration = 4000; // ms
        public const byte neckMotorDutyCycle = 255;
        public const int armTravelDuration = 6000; //ms
        public const byte armMotorDutyCycle = 255; // 12v motor


        private ArduinoComPort arduinoPort = new ArduinoComPort();

        private byte stepperLeftDevice = 0;
        private byte stepperRightDevice = 1;

        private byte stepperLeftEnable = 5;     // Inverted enable
        private byte stepperRightEnable = 8;     // Inverted enable

        private float stepperLeftProgress = 0;
        private float stepperRightProgress = 0;

        private byte neckMotorPWMPin = 3;
        private byte neckMotorDirPin = 12;

        private byte selfieBoomPWMPin = 11;
        private byte selfieDirPin = 13;

        private byte ledClockPin = 10;
        private byte ledDataPin = 9;

        private bool neckextended = false;
        private bool armextended = false;

        private int outstandingMovesLeft = 0;
        private int outstandingMovesRight = 0;

        private uint runDistance = (uint)(100 * stepsPerCM); //cm
        private uint rampDistance = (uint)(5 * stepsPerCM); //cm
        private double previousLeftThrottle = 0.0;
        private double previousRightThrottle = 0.0;

        private double lowEndCutOff = .2;

        public async Task ConnectToArduino()
        {
            await arduinoPort.GetDevice();

            await arduinoPort.SendStepperConfig(stepperLeftDevice, stepsPerRotation, 2, 4);
            await arduinoPort.SendStepperConfig(stepperRightDevice, stepsPerRotation, 6, 7);

            await arduinoPort.SetPinMode(stepperLeftEnable, ArduinoComPort.PinMode.Output);
            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);

            await arduinoPort.SetPinMode(stepperRightEnable, ArduinoComPort.PinMode.Output);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);

            await arduinoPort.SendLEDStripConfig(ledClockPin, ledDataPin);

            await arduinoPort.SetPinMode(neckMotorDirPin, ArduinoComPort.PinMode.Output);
            await arduinoPort.SetPinMode(neckMotorPWMPin, ArduinoComPort.PinMode.PWM);

            await arduinoPort.SetPinMode(selfieDirPin, ArduinoComPort.PinMode.Output);
            await arduinoPort.SetPinMode(selfieBoomPWMPin, ArduinoComPort.PinMode.PWM);
        }

        public bool HasArduino
        {
            get { return arduinoPort.IsConnected;  }
            private set { }
        }

        public bool NeckExtended
        {
            get { return neckextended; }
            private set { }
        }

        public bool ArmExtended
        {
            get { return armextended; }
            private set { }
        }

        public float StepperLeftProgress
        {
            get { return stepperLeftProgress; }
            private set { }
        }
        public float StepperRightProgress
        {
            get { return stepperRightProgress; }
            private set { }
        }
        public float MoveProgress
        {
            get
            {
                float progress = StepperLeftProgress * StepperRightProgress;
                return stepperRightProgress;
            }
            private set { }
        }

        public bool IsMoving
        {
            get { return outstandingMovesLeft > 0 || outstandingMovesRight > 0;  }
            private set { }

        }

        private float arcLength(float deg, float radius)
        {
            return (float)((Math.PI * radius * deg) / 180.0f);
        }

        public async Task Move(float distance)
        {
            await Move(distance, distance);
        }

        public async Task MoveAnalog(double xAnalog, double yAnalog)
        {
            // Convert from analog positions to left/right distances

            if (Math.Abs(xAnalog) < lowEndCutOff)
            {
                xAnalog = 0.0;
            }

            if (Math.Abs(yAnalog) < lowEndCutOff)
            {
                yAnalog = 0.0;
            }

            double leftMotorThrottle = yAnalog + xAnalog;
            double rightMotorThrottle = yAnalog - xAnalog;

            if (leftMotorThrottle > 1.0)
            {
                leftMotorThrottle = 1.0;
            }

            if (rightMotorThrottle > 1.0)
            {
                rightMotorThrottle = 1.0;
            }

            Debug.WriteLine("Throttle: " + leftMotorThrottle + "x" + rightMotorThrottle);

            outstandingMovesRight = 1;
            outstandingMovesLeft = 1;
            short leftSpeed;

            uint leftRunDistance = runDistance;
            uint rightRunDistance = runDistance;

            byte leftDirection = 1;
            if (leftMotorThrottle < 0)
            {
                leftDirection = 0;
            }


            if (Math.Abs(leftMotorThrottle) < lowEndCutOff)
            {
                if (previousLeftThrottle < 0.0)
                {
                    leftDirection = 0;
                }

                leftSpeed = (short)(Math.Abs(previousLeftThrottle) * maxSpeed);
                leftRunDistance = rampDistance;
                // Ramp to zero.
            }
            else
            {
                leftSpeed = (short)(Math.Abs(leftMotorThrottle) * maxSpeed);
            }

            if (Math.Abs(previousLeftThrottle) < lowEndCutOff)
            {
                // Letting it coast to zero
                Debug.WriteLine("Ramping left to zero");
            }
            else
            {

                await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
                await arduinoPort.SendStepperStep(stepperLeftDevice, leftDirection, leftRunDistance, leftSpeed, acceleration,
                    async () =>
                    {
                        outstandingMovesLeft--;
                        await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
                    },
                    (float progress) =>
                    {
                        stepperLeftProgress = progress;
                    });
            }

            short rightSpeed;
            byte rightDirection = 1;
            if (rightMotorThrottle < 0)
            {
                rightDirection = 0;
            }

            if (Math.Abs(rightMotorThrottle) < lowEndCutOff)
            {
                if (previousRightThrottle < 0.0)
                {
                    rightDirection = 0;
                }

                rightSpeed = (short)(Math.Abs(previousRightThrottle) * maxSpeed);
                rightRunDistance = rampDistance;

                // Ramp to zero.
            }
            else
            {
                rightSpeed = (short)(Math.Abs(rightMotorThrottle) * maxSpeed);
            }

            if (Math.Abs(previousRightThrottle) < lowEndCutOff)
            {
                // Letting it coast to zero
                Debug.WriteLine("Ramping right to zero");
            }
            else
            {
                await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.Low);
                await arduinoPort.SendStepperStep(stepperRightDevice, rightDirection, rightRunDistance, rightSpeed, acceleration,
                    async () =>
                    {
                        outstandingMovesRight--;
                        await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);
                    },
                    (float progress) =>
                    {
                        stepperRightProgress = progress;
                    });
            }

            previousLeftThrottle = leftMotorThrottle;
            previousRightThrottle = rightMotorThrottle;
        }

        public async Task Move(float rightDistance, float leftDistance)
        {
            byte rightDirection = 1;
            byte leftDirection = 1;
            if (rightDistance < 0)
            {
                rightDirection = 0;
                rightDistance = Math.Abs(rightDistance);
            }
            if (leftDistance < 0)
            {
                leftDirection = 0;
                leftDistance = Math.Abs(leftDistance);
            }

            var distanceL = stepsPerCM * leftDistance;
            var distanceR = stepsPerCM * rightDistance;

            var distL = (uint)Math.Floor(distanceL);
            var distR = (uint)Math.Floor(distanceR);

            outstandingMovesLeft = 1;
            outstandingMovesRight = 1;

            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.Low);

            await arduinoPort.SendStepperStep(stepperLeftDevice, leftDirection, distL, maxSpeed, acceleration,
                async () =>
                {
                    outstandingMovesLeft--;
                    await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
                },
                (float progress) =>
                {
                    stepperLeftProgress = progress;
                });

            await arduinoPort.SendStepperStep(stepperRightDevice, rightDirection, distR, maxSpeed, acceleration,
                async () =>
                {
                    outstandingMovesRight--;
                    await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);
                },
                (float progress) =>
                {
                    stepperRightProgress = progress;
                });
        }

        public async Task Rotate(float degrees)
        {
            var lengthInCM = arcLength(degrees, wheelBaseRadius);
            await Move(-lengthInCM, lengthInCM);
        }

        public async Task Stop()
        {
            // Stop is used for emergencies during run, but not a primary scenaro. For now, toggle enable
            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);
        }

        public async Task SetLedColor(byte r, byte g, byte b)
        {
            await arduinoPort.SetLEDStripColor(r, g, b);
        }

        public async Task RaiseNeck(int duration = neckTravelDuration)
        {
            if (!neckextended || duration != neckTravelDuration)
            {
                await arduinoPort.DigitalWrite(neckMotorDirPin, ArduinoComPort.PinState.High);
                await arduinoPort.AnalogWrite(neckMotorPWMPin, neckMotorDutyCycle);
                await Task.Delay(duration); // Yucky, could require some tuning
                await StopNeck();

                // If you are passing a duration, then we won't lock it.
                if (duration == neckTravelDuration)
                {
                    neckextended = true;
                }
            }
        }

        public async Task LowerNeck(int duration = neckTravelDuration)
        {
            if (neckextended || duration != neckTravelDuration)
            {
                await arduinoPort.DigitalWrite(neckMotorDirPin, ArduinoComPort.PinState.Low);
                await arduinoPort.AnalogWrite(neckMotorPWMPin, neckMotorDutyCycle);
                await Task.Delay(duration); // Yucky, could require some tuning
                await StopNeck();

                // If you are passing a duration, then we won't lock it.
                if (duration == neckTravelDuration)
                {
                    neckextended = false;
                }
            }
        }

        public async Task StopNeck()
        {
            await arduinoPort.AnalogWrite(neckMotorPWMPin, 0);
        }

        public async Task RaiseArm(int duration = armTravelDuration)
        {
            if (!armextended || duration != armTravelDuration)
            {
                await arduinoPort.DigitalWrite(selfieDirPin, ArduinoComPort.PinState.High);
                await arduinoPort.AnalogWrite(selfieBoomPWMPin, armMotorDutyCycle);
                await Task.Delay(duration); // Yucky, could require some tuning
                await StopArm();

                // If you are passing a duration, then we won't lock it.
                if (duration == armTravelDuration)
                {
                    armextended = true;
                }
            }
        }

        public async Task LowerArm(int duration = armTravelDuration)
        {
            if (armextended || duration != armTravelDuration)
            {
                await arduinoPort.DigitalWrite(selfieDirPin, ArduinoComPort.PinState.Low);
                await arduinoPort.AnalogWrite(selfieBoomPWMPin, armMotorDutyCycle);
                await Task.Delay(duration); // Yucky, could require some tuning
                await StopArm();
                // If you are passing a duration, then we won't lock it.
                if (duration == armTravelDuration)
                {
                    armextended = false;
                }
            }
        }

        public async Task StopArm()
        {
            await arduinoPort.AnalogWrite(selfieBoomPWMPin, 0);
        }

    }
}
