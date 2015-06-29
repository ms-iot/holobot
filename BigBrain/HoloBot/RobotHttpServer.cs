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
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Diagnostics;

namespace HoloBot
{
    public sealed class RobotHttpServer : IDisposable
    {
        private const uint bufLen = 8192;
        private int defaultPort = 3000;
        private readonly StreamSocketListener sock;
        internal HoloLensRobot bot;
        private string lastCommand = string.Empty;
        private const string successMsg = "{success=\"ok\"}";

        internal RobotHttpServer(int serverPort, HoloLensRobot bot)
        {
            this.bot = bot;
            sock = new StreamSocketListener();
            sock.Control.KeepAlive = true;
            defaultPort = serverPort;
            sock.ConnectionReceived += async (s, e) => await ProcessRequestAsync(e.Socket);
        }

        internal async void StartServer()
        {
            await bot.ConnectToArduino();
            await sock.BindServiceNameAsync(defaultPort.ToString());
        }

        private async Task ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                // Read in the HTTP request, we only care about type 'GET'
                StringBuilder requestFull = new StringBuilder(string.Empty);
                using (IInputStream input = socket.InputStream)
                {
                    byte[] data = new byte[bufLen];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = bufLen;
                    while (dataRead == bufLen)
                    {
                        await input.ReadAsync(buffer, bufLen, InputStreamOptions.Partial);
                        requestFull.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = socket.OutputStream)
                {
                    try
                    {
                        if (requestFull.Length == 0)
                        {
                            throw (new Exception("WTF dude?"));
                        }

                        string requestStart = requestFull.ToString().Split('\n')[0];
                        string[] requestParts = requestStart.Split(' ');
                        string requestMethod = requestParts[0];
                        if (requestMethod.ToUpper() != "GET")
                        {
                            throw (new Exception("UNSUPPORTED HTTP REQUEST METHOD"));
                        }

                        string requestPath = requestParts[1];
                        var splits = requestPath.Split('?');
                        if (splits.Length < 2)
                        {
                            throw (new Exception("EMPTY OR MISSING QUERY STRING"));
                        }

                        string botCmd = splits[1].ToLower();
                        if (string.IsNullOrEmpty(botCmd))
                        {
                            throw (new Exception("EMPTY OR MISSING QUERY STRING"));
                        }

                        WwwFormUrlDecoder queryBag = new WwwFormUrlDecoder(botCmd);
                        await ProcessCommand(queryBag, output);
                    }
                    catch (Exception e)
                    {
                        // We use 'Bad Request' here since chances are the exception was caused by bad query strings
                        await WriteResponseAsync("400 Bad Request", e.Message + e.StackTrace, output);
                    }
                }
            }
            catch (Exception e)
            {
                // Server can force shutdown which generates an exception. Spew it.
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
        }

        private async Task ProcessCommand(WwwFormUrlDecoder querybag, IOutputStream outstream)
        {
            try
            {
                if (!bot.HasArduino)
                {
                    throw new IOException("No Arduino Device Connected");
                }

                string botCmd = querybag.GetFirstValueByName("cmd"); // Throws System.ArgumentException if not found
                switch (botCmd)
                {
                    case "stop":
                        {
                            await bot.Stop();
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "status":
                        {
                            if (bot.IsMoving)
                            {
                                float moveProgress = bot.MoveProgress;
                                var moveProg = Math.Floor(moveProgress * 100.0f);

                                string s = "{command=\"" + lastCommand + "\", percent=\"" + moveProg.ToString() + "\"}";
                                await WriteResponseAsync("200 OK", s, outstream);
                            }
                            else
                            {
                                string s = "{command=\"" + lastCommand + "\"}";
                                await WriteResponseAsync("200 OK", s, outstream);
                            }

                            break;
                        }
                    case "move":
                        {
                            float dst = float.Parse(querybag.GetFirstValueByName("dst"));
                            await bot.Move(dst);
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "movelr":
                        {
                            float dstl = float.Parse(querybag.GetFirstValueByName("dstl"));
                            float dstr = float.Parse(querybag.GetFirstValueByName("dstr"));
                            await bot.Move(dstl, dstr);
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "setcolor":
                        {
                            byte r = 0;
                            byte g = 0;
                            byte b = 0;

                            // queryBag will throw an exception if it doesn't find it. 
                            // And you can't query if it is there.
                            try
                            {
                                r = byte.Parse(querybag.GetFirstValueByName("r"));
                            }
                            catch
                            { }

                            try
                            {
                                g = byte.Parse(querybag.GetFirstValueByName("g"));
                            }
                            catch
                            { }

                            try
                            {
                                b = byte.Parse(querybag.GetFirstValueByName("b"));
                            }
                            catch
                            { }

                            await bot.SetLedColor(r, g, b);
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "rotate":
                        {
                            float deg = float.Parse(querybag.GetFirstValueByName("deg"));
                            await bot.Rotate(deg);
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "neckextend":
                        {
                            await bot.RaiseNeck();
                            await WriteResponseAsync("200 OK", botCmd, outstream);
                            break;
                        }
                    case "neckretract":
                        {
                            await bot.LowerNeck();
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "armextend":
                        {
                            await bot.RaiseArm();
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }

                    case "armretract":
                        {
                            await bot.LowerArm();
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "neckextendtime":
                        {
                            int time = int.Parse(querybag.GetFirstValueByName("ms"));
                            await bot.RaiseNeck(time);

                            await WriteResponseAsync("200 OK", botCmd, outstream);
                            break;
                        }
                    case "neckretracttime":
                        {
                            int time = int.Parse(querybag.GetFirstValueByName("ms"));
                            await bot.LowerNeck(time);

                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "armextendtime":
                        {
                            int time = int.Parse(querybag.GetFirstValueByName("ms"));
                            await bot.RaiseArm(time);

                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "armretracttime":
                        {
                            int time = int.Parse(querybag.GetFirstValueByName("ms"));
                            await bot.LowerArm(time);

                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "camcapture":
                        {
                            await WriteResponseAsync("400 OK", successMsg, outstream);
                            break;
                        }
                    case "getbattery":
                        {
                            string s = "{success=\"ok\", \"percent\"=\"50\"}";
                            await WriteResponseAsync("200 OK", s, outstream);
                            break;
                        }
                    case "getwifi":
                        {
                            string s = "{success=\"ok\", \"percent\"=\"90\"}";
                            await WriteResponseAsync("200 OK", s, outstream);
                            break;
                        }
                    case "getcompass":
                        {
                            string s = "{success=\"ok\", \"heading\"=\"90\"}";
                            await WriteResponseAsync("200 OK", s, outstream);
                            break;
                        }
                    case "getaltitude":
                        {
                            string s = "{success=\"ok\", \"altitude\"=\"2400m\"}";
                            await WriteResponseAsync("200 OK", s, outstream);
                            break;
                        }
                    case "gettemp":
                        {
                            string s = "{success=\"ok\", \"temp\"=\"72f\"}";
                            await WriteResponseAsync("200 OK", s, outstream);
                            break;
                        }
                    case "shutdown":
                        {
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            // how do I initiate shutdown?
                            break;
                        }
                    default:
                        {
                            await WriteResponseAsync("400 Bad Request", string.Format("UNSUPPORTED COMMAND: {0}", botCmd), outstream);
                            break;
                        }
                }

                lastCommand = botCmd;
            }
            catch(ArgumentException)
            {
                await WriteResponseAsync("400 Bad Request", "INVALID QUERY STRING", outstream);
            }
            catch (Exception e)
            {
                await WriteResponseAsync("500 Internal Server Error", e.Message + e.StackTrace, outstream);
            }
        }

        private async Task WriteResponseAsync(string statuscode, string response, IOutputStream outstream)
        {
            using (DataWriter writer = new DataWriter(outstream))
            {
                try
                {
                    string respBody = string.IsNullOrEmpty(response) ? string.Empty : response;
                    string statCode = string.IsNullOrEmpty(statuscode) ? "200 OK" : statuscode;

                    string header = String.Format("HTTP/1.1 {0}\r\n" +
                                                  "Content-Type: text/html\r\n" +
                                                  "Content-Length: {1}\r\n" +
                                                  "Connection: close\r\n\r\n",
                                                  statuscode, response.Length);

                    writer.WriteString(header);
                    writer.WriteString(respBody);
                    await writer.StoreAsync();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message + "\n" + e.StackTrace);
                }
            }
        }

        public void Dispose()
        {
            sock.Dispose();
        }
    }
}
