using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace HoloBot
{
    public sealed class RobotHttpServer : IDisposable
    {
        private const uint bufLen = 8192;
        private int defaultPort = 50001;
        private readonly StreamSocketListener sock;
        internal HoloLensRobot bot;
        private string lastCommand = string.Empty;
        private const string successMsg = "{ success=\"ok\" }";

        internal RobotHttpServer(int serverPort, HoloLensRobot bot)
        {
            this.bot = bot;
            sock = new StreamSocketListener();
            defaultPort = serverPort;
            sock.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
        }

        internal async void StartServer()
        {
            await bot.ConnectToArduino();
            await sock.BindServiceNameAsync(defaultPort.ToString());
        }

        private async void ProcessRequestAsync(StreamSocket socket)
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
                    string requestStart = requestFull.ToString().Split('\n')[0];
                    string[] requestParts = requestStart.Split(' ');
                    string requestMethod = requestParts[0];
                    if (requestMethod.ToUpper() != "GET")
                    {
                        throw (new Exception("UNSUPPORTED HTTP REQUEST METHOD"));
                    }

                    string requestPath = requestParts[1];
                    string botCmd = requestPath.Split('?')[1].ToLower();
                    if(string.IsNullOrEmpty(botCmd))
                    {
                        throw (new Exception("EMPTY OR MISSING QUERY STRING"));
                    }

                    WwwFormUrlDecoder queryBag = new WwwFormUrlDecoder(botCmd);
                    ProcessCommand(queryBag, output);
                }
                catch (Exception e)
                {
                    // We use 'Bad Request' here since chances are the exception was caused by bad query strings
                    await WriteResponseAsync("400 Bad Request", e.Message + e.StackTrace, output);
                }
            }
        }

        private async void ProcessCommand(WwwFormUrlDecoder querybag, IOutputStream outstream)
        {
            try
            {
                if (!bot.HasArduino)
                    throw new IOException("No Arduino Device Connected");

                string botCmd = querybag.GetFirstValueByName("command"); // Throws System.ArgumentException if not found
                switch (botCmd)
                {
                    case "stop":
                        {
                            bot.Stop();
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
                            bot.Move(dst);
                            await WriteResponseAsync("200 OK", successMsg, outstream);
                            break;
                        }
                    case "movelr":
                        {
                            float dstl = float.Parse(querybag.GetFirstValueByName("dstl"));
                            float dstr = float.Parse(querybag.GetFirstValueByName("dstr"));
                            bot.Move(dstl, dstr);
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
                            bot.Rotate(deg);
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
            string respBody = string.IsNullOrEmpty(response) ? string.Empty : response;
            string statCode = string.IsNullOrEmpty(statuscode) ? "200 OK" : statuscode;

            using (Stream resp = outstream.AsStreamForWrite())
            {
                byte[] bodyArray = Encoding.UTF8.GetBytes(respBody);
                MemoryStream stream = new MemoryStream(bodyArray);

                string header = String.Format("HTTP/1.1 {0}\r\n" +
                                              "Content-Type: text/html\r\n" +
                                              "Content-Length: {1}\r\n" +
                                              "Connection: close\r\n\r\n",
                                              statuscode, stream.Length);

                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }
        }
        
        public void Dispose()
        {
            sock.Dispose();
        }
    }
}
