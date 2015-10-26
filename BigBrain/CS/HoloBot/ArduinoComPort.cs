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
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace HoloBot
{
    internal class ArduinoComPort
    {
        enum FirmataCommand : byte
        {
            PinMode = 0xF4,
            DigitalWrite = 0x90,
            AnalogWrite = 0xE0
        };

        enum SysEx : byte
        {
            Start = 0xF0,
            End = 0xF7,
            StepperCommand = 0x72,
            StepperProgress = 0x73,
            LEDStripConfigCommand = 0x74,
            LEDStripColorCommand = 0x75
        };

        enum StepperCommand : byte
        {
            Config = 0,
            Step = 1
        }

        public enum PinMode : byte
        {
            Input = 0,
            Output = 1,
            PWM = 3,
        };

        public enum PinState : byte
        {
            Low = 0,
            High = 1
        };

        struct PinData
        {
            public PinMode mode;
            public PinState state;
            public short duty;
        };

        private const uint PinCount = 20;
        private const uint MaxSysexSize = 512;
        private PinData[] pinData = new PinData[PinCount];


        private SerialDevice arduinoPort = null;
        private DataWriter writer = null;
        private DataReader reader = null;

        public delegate void StepperComplete();
        public delegate void StepperProgress(float progress);

        private StepperComplete[] stepperComplete = new StepperComplete[5];
        private StepperProgress[] stepperProgress = new StepperProgress[5];


        // NOTE: ASSUMES ONLY ONE ARDUINO DEVICE IS CONNECTED, FIRST FOUND WINS
        public async Task GetDevice(string identifyingSubStr = "VID_2341")
        {
            arduinoPort = null;
            string selector = SerialDevice.GetDeviceSelector();
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
                        arduinoPort.BaudRate = 115200;
                        arduinoPort.Parity = SerialParity.None;
                        arduinoPort.DataBits = 8;
                        arduinoPort.StopBits = SerialStopBitCount.One;
                        arduinoPort.Handshake = SerialHandshake.None;
                        arduinoPort.ReadTimeout = TimeSpan.FromSeconds(5);
                        arduinoPort.WriteTimeout = TimeSpan.FromSeconds(5);
                        arduinoPort.Handshake = SerialHandshake.RequestToSendXOnXOff;
                        arduinoPort.IsDataTerminalReadyEnabled = true;

                        writer = new DataWriter(arduinoPort.OutputStream);
                        reader = new DataReader(arduinoPort.InputStream);

                        startWatchingResponses();

                        return;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get { return arduinoPort != null; }
            private set { }
        }

        public async Task SetPinMode(byte pin, PinMode mode)
        {
            pinData[pin].mode = mode;

            byte[] commandBuffer =
            {
                (byte)FirmataCommand.PinMode,
                pin,
                (byte)mode
            };

            await WriteData(commandBuffer);
        }

        public async Task DigitalWrite(byte pin, PinState state)
        {
            pinData[pin].state = state;
            byte port = (byte)(pin / 8);
            int portValue = 0;

            for (var i = 0; i < 8; i++)
            {
                if (pinData[8 * port + i].state == PinState.High)
                {
                    portValue |= (1 << i);
                }
            }

            byte[] commandBuffer =
            {
                (byte)(((byte)(FirmataCommand.DigitalWrite)) | port),
                (byte)(portValue & 0x7f),
                (byte)((portValue >> 7) & 0x7F)
            };

            await WriteData(commandBuffer);
        }

        public async Task AnalogWrite(byte pin, short dutycycle)
        {
            pinData[pin].duty = dutycycle;

            byte[] commandBuffer =
            {
                (byte)(((byte)(FirmataCommand.AnalogWrite)) | pin),
                (byte)(dutycycle & 0x7f),
                (byte)((dutycycle >> 7) & 0x7F)
            };

            await WriteData(commandBuffer);
        }

        public async Task SendStepperConfig(byte deviceNumber, short stepsPerRev, byte dirPin, byte stepPin)
        {
            byte[] commandBuffer = 
            {
                (byte)SysEx.Start,
                (byte)SysEx.StepperCommand,
                (byte)StepperCommand.Config,
                deviceNumber,
                0x1,    // type
                (byte)(stepsPerRev & 0x7F),
                (byte)((stepsPerRev >> 7) & 0x7F),
                dirPin,
                stepPin,
                (byte)SysEx.End
            };

            await WriteData(commandBuffer);
        }

        public async Task SendStepperStep(byte deviceNumber, byte direction, uint steps, short speed, short acceleration, StepperComplete completion, StepperProgress progress)
        {
            byte[] commandBuffer =
            {
                (byte)SysEx.Start,
                (byte)SysEx.StepperCommand,
                (byte)StepperCommand.Step,
                deviceNumber,
                direction,
                (byte)(steps & 0x7F),
                (byte)((steps >> 7) & 0x7F),
                (byte)((steps >> 14) & 0x7F),
                (byte)(speed & 0x7F),
                (byte)((speed >> 7) & 0x7F),
                (byte)(acceleration & 0x7F),
                (byte)((acceleration >> 7) & 0x7F),
                (byte)(acceleration & 0x7F),            // For our purposes, acceleration and deceleration are equal.
                (byte)((acceleration >> 7) & 0x7F),
                (byte)SysEx.End
            };

            stepperComplete[deviceNumber] = completion;
            stepperProgress[deviceNumber] = progress;

            await WriteData(commandBuffer);
        }

        public async Task SendLEDStripConfig(byte clockPin, byte dataPin)
        {
            byte[] commandBuffer =
            {
                (byte)SysEx.Start,
                (byte)SysEx.LEDStripConfigCommand,
                clockPin,
                dataPin,
                (byte)SysEx.End
            };

            await WriteData(commandBuffer);
        }
        public async Task SetLEDStripColor(byte r, byte g, byte b)
        {
            byte[] commandBuffer =
            {
                (byte)SysEx.Start,
                (byte)SysEx.LEDStripColorCommand,
                r,
                g,
                b,
                (byte)SysEx.End
            };

            await WriteData(commandBuffer);
        }

        public async Task WriteData(byte[] data)
        {
            if (!IsConnected)
                return;

            writer.WriteBytes(data);
            await writer.StoreAsync();
        }

        public async Task WriteString(string dataStr)
        {
            if (!IsConnected)
                return;

            byte[] data = System.Text.Encoding.UTF8.GetBytes(dataStr);
            writer.WriteBytes(data);
            await writer.StoreAsync();
        }

        public async Task<string> ReadStringAsync()
        {
            if (!IsConnected)
                return string.Empty;

            uint bufLen = 1024;
            DataReader reader = new DataReader(arduinoPort.InputStream);

            uint bytesRead = await reader.LoadAsync(bufLen);

            return (bytesRead > 0) ? reader.ReadString(bytesRead) : string.Empty;
        }

        private void startWatchingResponses()
        {
            Task t = Task.Run(async () =>
            {
                byte[] sysexBuffer = new byte[MaxSysexSize];
                uint offset = 0;
                while (true)
                {
                    var result = await reader.LoadAsync(1);
                    while (reader.UnconsumedBufferLength > 0)
                    {
                        sysexBuffer[offset] = reader.ReadByte();

                        if (sysexBuffer[offset] == (byte)SysEx.End)
                        {
                            // Got a sysex message.
                            //Dispatch

                            switch (sysexBuffer[1])
                            {
                                case (byte)SysEx.StepperCommand:
                                    {
                                        var deviceNumber = sysexBuffer[2];
                                        stepperComplete[deviceNumber]();
                                    }
                                    break;
                                case (byte)SysEx.StepperProgress:
                                    {
                                        var deviceNumber = sysexBuffer[2];
                                        var progress = sysexBuffer[3]; // this is 0 - 100; convert to 0 - 1 for easy math on this side.
                                        float fProgress = progress / 100.0f;
                                        stepperProgress[deviceNumber](fProgress);
                                    }
                                    break;
                                default:
                                    {
                                        Debug.WriteLine(String.Format("Received a Sysex Response which I'm not handling: {0}", sysexBuffer[1]));
                                    }
                                    break;
                            }

                            offset = 0;
                        }
                        else
                        {
                            offset++;
                        }
                    }
                }
            });
        }
    }
}
