using System;
using System.Threading.Tasks;

namespace HoloBot
{
    class HoloLensRobot
    {
        public const float wheelDiameter = 16.8F; // CM
        public const float wheelCircumference = (float)(Math.PI * wheelDiameter); // CM
        public const byte microStep = 1;
        public const uint stepsPerRotation = (200 * microStep);
        public const float downGear = 18/90;
        public const float stepsPerCM = stepsPerRotation / wheelCircumference / downGear;
        public const float wheelBase = 38.1F; // CM
        public const float wheelBaseRadius = wheelBase / 2.0F;

        private ArduinoComPort arduinoPort = new ArduinoComPort();

        public async Task ConnectToArduino()
        {
            await arduinoPort.GetDevice();
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

        public void Move(float distance)
        {
            string cmd = string.Format("move:{0};;", distance.ToString());
            arduinoPort.WriteString(cmd);
        }

        public void Move(float rightDistance, float leftDistance)
        {
            string cmd = string.Format("movelr:{0};{1};;", leftDistance.ToString(), rightDistance.ToString());
            arduinoPort.WriteString(cmd);
        }

        public void Rotate(float degrees)
        {
            string cmd = string.Format("rotate:{0};;", degrees.ToString());
            arduinoPort.WriteString(cmd);
        }

        public void Stop()
        {
            arduinoPort.WriteString("stopdrive:;;");
        }

        public void SetLedColor(byte r, byte g, byte b)
        {
            string cmd = string.Format("setled:{0};{1};{2};;", r.ToString(), g.ToString(), b.ToString());
            arduinoPort.WriteString(cmd);
        }

        public void RaiseNeck()
        {
            arduinoPort.WriteString("raiseneck:;;");
        }

        public void LowerNeck()
        {
            arduinoPort.WriteString("lowerneck:;;");
        }

        public void StopNeck()
        {
            arduinoPort.WriteString("stopneck:;;");
        }
    }
}
