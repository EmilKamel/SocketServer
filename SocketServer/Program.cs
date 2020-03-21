using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class RemoteHostState
    {
        private static readonly Random Random = new Random();
        public string RandomRemoteHostStateName;
        public Socket WorkSocket;
        public const int BufferSize = 1024;
        public readonly byte[] Buffer = new byte[BufferSize];
        public readonly StringBuilder ReceivedDataString = new StringBuilder();

        public RemoteHostState()
        {
            RandomRemoteHostStateName = RandomString(8);
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
    
    public static class SocketListener
    {
        private static readonly ManualResetEvent ResetEvent = new ManualResetEvent(false);

        private static void StartListening()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    ResetEvent.Reset();
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(AcceptCallback, listener);
                    
                    ResetEvent.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            ResetEvent.Set();
            
            Socket listener = (Socket) ar.AsyncState;
            Socket remoteHostHandler = listener.EndAccept(ar);
            
            RemoteHostState remoteHostState = new RemoteHostState {WorkSocket = remoteHostHandler};
            remoteHostHandler.BeginReceive(remoteHostState.Buffer, 0, RemoteHostState.BufferSize, 0, ReadCallback, remoteHostState);
        }

        private static void ReadCallback(IAsyncResult ar)
        {
            RemoteHostState remoteHostState = (RemoteHostState) ar.AsyncState;

            int bytesRead = remoteHostState.WorkSocket.EndReceive(ar);

            if (bytesRead > 0)
            {
                remoteHostState.ReceivedDataString.Append(Encoding.ASCII.GetString(remoteHostState.Buffer, 0, bytesRead));

                var message = remoteHostState.ReceivedDataString.ToString();
                remoteHostState.ReceivedDataString.Clear();
                
                Console.WriteLine($"T: {Thread.CurrentThread.ManagedThreadId} S: {remoteHostState.RandomRemoteHostStateName} Client Message : {message}");
                
                if (message.IndexOf("<ROGER>", StringComparison.Ordinal) > -1)
                {
                    Console.WriteLine($"Read {message.Length} bytes from socket. \n Data : {message}");
                }
                else
                {
                    remoteHostState.WorkSocket.BeginReceive(remoteHostState.Buffer, 0, RemoteHostState.BufferSize, 0, ReadCallback, remoteHostState);
                }
            }
        }

        public static int Main()
        {
            StartListening();
            return 0;
        }
    }
}