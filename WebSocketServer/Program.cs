using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;


namespace WebSocketServer
{
    class Program
    {

        public static ManualResetEvent allDone = new ManualResetEvent(false);
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork,
        SocketType.Stream, ProtocolType.Tcp);
        static private string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        static void Main(string[] args)
        {
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 8080));
            serverSocket.Listen(128);
            serverSocket.BeginAccept(OnAccept, null);
            Console.Read();

        }


        public static String DecodeMessage(Byte[] bytes)
        {
            String incomingData = String.Empty;
            Byte secondByte = bytes[1];
            Int32 dataLength = secondByte & 127;
            Int32 indexFirstMask = 2;
            if (dataLength == 126)
                indexFirstMask = 4;
            else if (dataLength == 127)
                indexFirstMask = 10;

            IEnumerable<Byte> keys = bytes.Skip(indexFirstMask).Take(4);
            Int32 indexFirstDataByte = indexFirstMask + 4;

            Byte[] decoded = new Byte[bytes.Length - indexFirstDataByte];
            for (Int32 i = indexFirstDataByte, j = 0; i < bytes.Length; i++, j++)
            {
                decoded[j] = (Byte)(bytes[i] ^ keys.ElementAt(j % 4));
            }

            return incomingData = Encoding.UTF8.GetString(decoded, 0, decoded.Length);
        }

        private static void OnAccept(IAsyncResult result)
        {
            try
            {
                byte[] buffer = new byte[1024];
                try
                {
                    Socket client = null;

                    
                    string headerResponse = "";
                    if (serverSocket != null && serverSocket.IsBound)
                    {
                        client = serverSocket.EndAccept(result);
                        var i = client.Receive(buffer);
                        headerResponse = (System.Text.Encoding.UTF8.GetString(buffer)).Substring(0, i);
                        // write received data to the console
                        Console.WriteLine(headerResponse);

                    }
                    if (client != null)
                    {
                        /* Handshaking and managing ClientSocket */

                        var key = headerResponse.Replace("Sec-WebSocket-Key: ", "`").Replace("Sec-WebSocket-Extensions:", "`")
                                  .Split('`')[1]                     // dGhlIHNhbXBsZSBub25jZQ== \r\n .......
                                  .Replace("\r", "").Split('\n')[0]  // dGhlIHNhbXBsZSBub25jZQ==
                                  .Trim();

                        // key should now equal dGhlIHNhbXBsZSBub25jZQ==
                        var test1 = AcceptKey(ref key);

                        var newLine = "\r\n";

                        var response =
                    "HTTP/1.1 101 WebSocket Protocol Handshake" + Environment.NewLine +
                    "Upgrade: WebSocket" + Environment.NewLine +
                    "Connection: Upgrade" + Environment.NewLine +
                    "Sec-WebSocket-Origin: http://localhost:3000"  + Environment.NewLine +
                    "Sec-WebSocket-Location: ws://localhost:8080" + Environment.NewLine +
                   // "Sec-WebSocket-Protocol: json" + Environment.NewLine +
                    "Sec-WebSocket-Accept: " + test1 + Environment.NewLine + Environment.NewLine ;


                        /*var response = "HTTP/1.1 101 Switching Protocols" + newLine
                             + "Upgrade: websocket" + newLine
                             + "Connection: Upgrade" + newLine
                             + "Sec-WebSocket-Accept: " + test1 + newLine + newLine
                             //+ "Sec-WebSocket-Protocol: chat, superchat" + newLine
                             //+ "Sec-WebSocket-Version: 13" + newLine
                             ;*/

                        // which one should I use? none of them fires the onopen method
                        client.Send(System.Text.Encoding.UTF8.GetBytes(response));
                        //client.Send(System.Text.Encoding.UTF8.GetBytes("dsadas"));
                        
                        while (true)
                        {
                            byte[] newBuffer = new byte[1024];
                            int ix = client.Receive(newBuffer); // wait for client to send a message

                            


                            //Console.WriteLine(DecodeMessage(newBuffer));

                            String[] responseString = DecodeMessage(newBuffer).Split(',');
                            string lastElement = responseString[responseString.Length - 1];


                            int charIndex = 0;
                            foreach (var item in lastElement)
                            {
                                
                                if (Char.IsDigit((char)item))
                                {
                                    charIndex++;
                                    continue;
                                }
                                else
                                {
                                    responseString[responseString.Length - 1] = lastElement.Substring(0, charIndex);
                                    break;
                                }
                                 

                            }
                            // once the message is received decode it in different formats
                            //Console.WriteLine(ix.ToString());
                            //Console.WriteLine((System.Text.Encoding.UTF8.GetString(newBuffer)));
                            
                            for (int i = 0; i < Convert.ToInt32(responseString[1]); i++)
                            {
                                string a = ((char)Convert.ToInt32(responseString[i+2])).ToString();
                                Console.Write(a);
                            }

                            Console.WriteLine();
                            

                        }
                       
                        



                    }
                }
                catch (Exception exception)
                {
                    throw exception;
                }
                finally
                {
                    if (serverSocket != null && serverSocket.IsBound)
                    {
                        serverSocket.BeginAccept(null, 0, OnAccept, null);
                    }
                }
            }
            catch (SocketException exception)
            {

            }
            finally
            {
                if (serverSocket != null && serverSocket.IsBound)
                {
                    serverSocket.BeginAccept(null, 0, OnAccept, null);
                }
            }
        }

        private string BufferToString(byte[] bytes)
        {
            string response = string.Empty;

            foreach (byte b in bytes)
                response += (Char)b;

            return response;
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private static string AcceptKey(ref string key)
        {
            string longKey = key + guid;
            byte[] hashBytes = ComputeHash(longKey);
            return Convert.ToBase64String(hashBytes);
        }

        static SHA1 sha1 = SHA1CryptoServiceProvider.Create();
        private static byte[] ComputeHash(string str)
        {
            return sha1.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));
        }
    }

}
