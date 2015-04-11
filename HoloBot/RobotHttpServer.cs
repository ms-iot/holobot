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
                    if(string.IsNullOrEmpty(botCmd))
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
                            string s = "{command=\"" + lastCommand + "\"}";
                            await WriteResponseAsync("200 OK", s, outstream);
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
                            byte r = byte.Parse(querybag.GetFirstValueByName("r"));
                            byte g = byte.Parse(querybag.GetFirstValueByName("g"));
                            byte b = byte.Parse(querybag.GetFirstValueByName("b"));
                            bot.SetLedColor(r, g, b);
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
                            bot.RaiseNeck();
                            await WriteResponseAsync("200 OK", botCmd, outstream);
                            break;
                        }
                    case "neckretract":
                        {
                            bot.LowerNeck();
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }

                    case "neckextendtime":
                    case "neckretracttime":
                    case "armextend":
                    case "armretract":
                    case "armextendtime":
                    case "armretracttime":
                    case "camcapture":
                    case "getbattery":
                    case "getwifi":
                    case "getcompass":
                    case "getaltitude":
                    case "gettemp":
                    case "shutdown":
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
