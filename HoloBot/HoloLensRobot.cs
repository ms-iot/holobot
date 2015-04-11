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

        private ArduinoComPort arduinoPort = new ArduinoComPort();

        private byte stepperLeftDevice = 0;
        private byte stepperRightDevice = 1;

        private byte stepperLeftEnable = 5;
        private byte stepperRightEnable = 8;


        private int outstandingMoves = 0;

        public async Task ConnectToArduino()
        {
            await arduinoPort.GetDevice();

            await arduinoPort.SendStepperConfig(stepperLeftDevice, stepsPerRotation, 2, 4);
            await arduinoPort.SendStepperConfig(stepperRightDevice, stepsPerRotation, 6, 7);
        }

        public bool HasArduino
        {
            get { return arduinoPort.IsConnected;  }
            private set { }
        }

        private double arcLength(float deg, float radius)
        {
            return (Math.PI * radius * deg) / 180;
        }

        public async Task Move(float distance)
        {
            byte direction = 0;
            if (distance < 0)
            {
                direction = 1;
                distance = Math.Abs(distance);
            }

            var distanceL = stepsPerCM * distance;
            var distanceR = stepsPerCM * distance;

            var distL = (uint)Math.Floor(distanceL);
            var distR = (uint)Math.Floor(distanceR);

            outstandingMoves = 2;

            await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.High);
            await arduinoPort.DigitalWrite(stepperRightEnable, ArduinoComPort.PinState.High);

            await arduinoPort.SendStepperStep(stepperLeftDevice, direction, distL, maxSpeed, acceleration, 
                async () =>
                {
                    outstandingMoves--;
                    await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
                });

            await arduinoPort.SendStepperStep(stepperRightDevice, direction, distR, maxSpeed, acceleration,
                async () =>
                {
                    outstandingMoves--;
                    await arduinoPort.DigitalWrite(stepperLeftEnable, ArduinoComPort.PinState.Low);
                });
        }

        public void Move(float rightDistance, float leftDistance)
        {
        }

        public void Rotate(float degrees)
        {
        }

        public void Stop()
        {
        }

        public void SetLedColor(byte r, byte g, byte b)
        {
        }

        public void RaiseNeck()
        {
        }

        public void LowerNeck()
        {
        }

        public void StopNeck()
        {
        }
    }
}
