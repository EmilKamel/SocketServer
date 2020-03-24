using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class ClientState
    {
        public readonly string RemoteClientHostName;
        public Socket RemoteClientHandlerSocket;
        public const int BufferSize = 1024;
        public readonly byte[] Buffer = new byte[BufferSize];
        public readonly StringBuilder ReceivedMessageBuilder = new StringBuilder();

        public ClientState()
        {
            RemoteClientHostName = RandomString(8);
        }

        private static string RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
    
    public static class Server
    {
        private static readonly ManualResetEvent ResetEvent = new ManualResetEvent(false);

        private static void StartListening()
        {
            var hostName = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = hostName.AddressList[0];
            var ipAddressAndPort = new IPEndPoint(ipAddress, 11000);

            var serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                serverSocket.Bind(ipAddressAndPort);
                serverSocket.Listen(100);

                while (true)
                {
                    ResetEvent.Reset();
                    serverSocket.BeginAccept(AcceptCallback, serverSocket);
                    Console.WriteLine("Awaiting client connection \n");
                    
                    ResetEvent.WaitOne();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        private static void AcceptCallback(IAsyncResult asyncResult)
        {
            ResetEvent.Set();
            
            var serverSocket = (Socket) asyncResult.AsyncState;
            var clientHandlerSocket = serverSocket.EndAccept(asyncResult);
            
            var clientState = new ClientState {RemoteClientHandlerSocket = clientHandlerSocket};
            clientHandlerSocket.BeginReceive(clientState.Buffer, 0, ClientState.BufferSize, 0, ReadCallback, clientState);
        }

        private static void ReadCallback(IAsyncResult asyncResult)
        {
            try
            {
                var clientState = (ClientState) asyncResult.AsyncState;

                var clientMessageBytes = clientState.RemoteClientHandlerSocket.EndReceive(asyncResult);

                if (clientMessageBytes <= 0) return;

                clientState.ReceivedMessageBuilder.Append(Encoding.ASCII.GetString(clientState.Buffer, 0, clientMessageBytes));
                var receivedClientMessage = clientState.ReceivedMessageBuilder.ToString();
                clientState.ReceivedMessageBuilder.Clear();

                var formattedClientMessage = $"T: {Thread.CurrentThread.ManagedThreadId} S: {clientState.RemoteClientHostName} Client Message: {receivedClientMessage}";
                Console.WriteLine(formattedClientMessage);
                
            }
            catch (Exception)
            {
                // ignored - Continue if client closes unexpectedly.
            }
        }

        public static int Main()
        {
            StartListening();
            return 0;
        }
    }
}