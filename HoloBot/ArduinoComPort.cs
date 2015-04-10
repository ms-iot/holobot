using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace HoloBot
{
    internal class ArduinoComPort
    {
        private SerialDevice arduinoPort;
        private bool isConnected = false;

        // NOTE: ASSUMES ONLY ONE ARDUINO DEVICE IS CONNECTED, FIRST FOUND WINS
        public async Task GetDevice(string selectorStr = "ALL", string identifyingSubStr = "VID_2341")
        {
            arduinoPort = null;
            string selector = SerialDevice.GetDeviceSelector(selectorStr);
            var deviceCollection = await DeviceInformation.FindAllAsync(selector);

            if (deviceCollection.Count == 0)
                return;

            for (int i = 0; i < deviceCollection.Count; ++i)
            {
                if (deviceCollection[i].Name.Contains(identifyingSubStr) || deviceCollection[i].Id.Contains(identifyingSubStr))
                {
                    arduinoPort = await SerialDevice.FromIdAsync(deviceCollection[i].Id);
                    if (arduinoPort != null)
                    {
                        arduinoPort.BaudRate = 9600;
                        arduinoPort.Parity = SerialParity.None;
                        arduinoPort.DataBits = 8;
                        arduinoPort.StopBits = SerialStopBitCount.One;
                        arduinoPort.Handshake = SerialHandshake.None;
                        arduinoPort.ReadTimeout = TimeSpan.FromSeconds(5);
                        arduinoPort.WriteTimeout = TimeSpan.FromSeconds(5);

                        isConnected = true;

                        return;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get { return IsConnected; }
            private set { }
        }

        public void WriteString(string dataStr)
        {
            if (!isConnected)
                return;

            byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
            DataWriter writer = new DataWriter();
            writer.WriteBytes(data);

            var bytesWritten = arduinoPort.OutputStream.WriteAsync(writer.DetachBuffer());
        }

        public async Task<string> ReadAsync()
        {
            if (!isConnected)
                return string.Empty;

            uint bufLen = 1024;
            DataReader reader = new DataReader(arduinoPort.InputStream);

            uint bytesRead = await reader.LoadAsync(bufLen);

            return (bytesRead > 0) ? reader.ReadString(bytesRead) : string.Empty;
        }
    }
}
