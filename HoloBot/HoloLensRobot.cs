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
        public const uint neckTravelDuration = 3000; // ms

        private ArduinoComPort arduinoPort = new ArduinoComPort();

        private byte stepperLeftDevice = 0;
        private byte stepperRightDevice = 1;

        private byte stepperLeftEnable = 5;     // Inverted enable
        private byte stepperRightEnable = 8;     // Inverted enable

        private byte ledClockPin = 10;
        private byte ledDataPin = 9;

        private bool neckextended = false;

        private int outstandingMoves = 0;

        public async Task ConnectToArduino()
        {
            await arduinoPort.GetDevice();

            await arduinoPort.SendStepperConfig(stepperLeftDevice, stepsPerRotation, 2, 4);
            await arduinoPort.SendStepperConfig(stepperRightDevice, stepsPerRotation, 6, 7);

            await arduinoPort.SendLEDStripConfig(ledClockPin, ledDataPin);

            await arduinoPort.SetPinMode(stepperLeftEnable, ArduinoComPort.PinMode.Output);
            await arduinoPort.SetPinMode(stepperRightEnable, ArduinoComPort.PinMode.Output);

            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);
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
                });

            await arduinoPort.SendStepperStep(stepperRightDevice, rightDirection, distR, maxSpeed, acceleration,
                async () =>
                {
                    outstandingMoves--;
                    await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
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

        public async Task RaiseNeck()
        {
            if (!neckextended)
            {
                await arduinoPort.RaiseNeck(neckTravelDuration);
            }
        }

        public async Task LowerNeck()
        {
            if (neckextended)
            {
                await arduinoPort.LowerNeck(neckTravelDuration);
            }
        }
    }
}
