using System;
using System.Threading.Tasks;

namespace HoloBot
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

        private int outstandingMoves = 0;

        public async Task ConnectToArduino()
        {
            await arduinoPort.GetDevice();

            await arduinoPort.SendStepperConfig(stepperLeftDevice, stepsPerRotation, 2, 4);
            await arduinoPort.SendStepperConfig(stepperRightDevice, stepsPerRotation, 6, 7);

            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);

            await arduinoPort.SendLEDStripConfig(ledClockPin, ledDataPin);

            await arduinoPort.SetPinMode(stepperLeftEnable, ArduinoComPort.PinMode.Output);
            await arduinoPort.SetPinMode(stepperRightEnable, ArduinoComPort.PinMode.Output);

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
            get { return outstandingMoves > 0;  }
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

        public async Task Move(float rightDistance, float leftDistance)
        {
            byte rightDirection = 0;
            byte leftDirection = 0;
            if (rightDistance < 0)
            {
                rightDirection = 1;
                rightDistance = Math.Abs(rightDistance);
            }
            if (leftDistance < 0)
            {
                leftDirection = 1;
                leftDistance = Math.Abs(leftDistance);
            }

            var distanceL = stepsPerCM * leftDistance;
            var distanceR = stepsPerCM * rightDistance;

            var distL = (uint)Math.Floor(distanceL);
            var distR = (uint)Math.Floor(distanceR);

            outstandingMoves = 2;

            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.Low);

            await arduinoPort.SendStepperStep(stepperLeftDevice, leftDirection, distL, maxSpeed, acceleration,
                async () =>
                {
                    outstandingMoves--;
                    await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
                },
                (float progress) =>
                {
                    stepperLeftProgress = progress;
                });

            await arduinoPort.SendStepperStep(stepperRightDevice, rightDirection, distR, maxSpeed, acceleration,
                async () =>
                {
                    outstandingMoves--;
                    await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
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
