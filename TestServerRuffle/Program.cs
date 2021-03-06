﻿//System
using System;
using System.Text;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections.Concurrent;
using System.Diagnostics;

//Ruffles
using Ruffles.Configuration;
using Ruffles.Channeling;
using Ruffles.Connections;
using Ruffles.Core;
using System.Collections.Generic;


namespace TestServerRuffle
{

    class Program
    {

        internal static readonly SocketConfig ServerConfig = new SocketConfig()
        {
            ChallengeDifficulty = 20, // Difficulty 20 is fairly hard
            ChannelTypes = new ChannelType[]
           {
                ChannelType.Reliable,
                ChannelType.ReliableSequenced,
                ChannelType.Unreliable,
                ChannelType.UnreliableOrdered,
                ChannelType.ReliableSequencedFragmented
           },
            DualListenPort = 5674,
            SimulatorConfig = new Ruffles.Simulation.SimulatorConfig()
            {
                DropPercentage = 0.05f,
                MaxLatency = 10,
                MinLatency = 0
            },
            UseSimulator = false
        };

        /// <summary>
        /// Client message received.
        /// </summary>
		public struct ClientMessageReceived
        {
            public byte[] MessageBytes;
            public Connection ClientConnected;
            public ulong clientIdConnected;
        };

        //https://stackoverflow.com/questions/8629285/how-to-create-a-collection-like-liststring-object
        static List<KeyValuePair<ulong, Connection>> clients = new List<KeyValuePair<ulong, Connection>>();
        //Queue Messages
        static ConcurrentQueue<ClientMessageReceived> QueueMessages = new ConcurrentQueue<ClientMessageReceived>();

        static RuffleSocket server = new RuffleSocket(ServerConfig);

        static ManualResetEvent _quitEvent = new ManualResetEvent(false);

        // The server stores the clients id here
        static ulong clientId = 0;
        static Connection clientConnection = null;

        // The time when the connection started
        DateTime started = DateTime.Now;

        // The time when the last message was sent
        DateTime lastSent = DateTime.MinValue;

        // The time the last status was printed
        DateTime lastStatusPrint = DateTime.MinValue;

        // The amount of message that has been received
        static int messagesReceived = 0;

        // The amount of messages that has been sent
        static int messageCounter = 0;

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            //Some Server Information
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            Console.WriteLine("Server Version: " + version);

            //Start Ruffles
            server.OnNetworkEvent += Server_OnNetworkEvent;
            server.Start();

            //https://stackoverflow.com/questions/2586612/how-to-keep-a-net-console-app-running
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            _quitEvent.WaitOne();

            // cleanup/shutdown and quit
            server.Stop();
            Environment.Exit(0);
        }

        /// <summary>
        /// Received event from clients
        /// </summary>
        /// <param name="obj"></param>
        private static void Server_OnNetworkEvent(NetworkEvent obj)
        {
            if (obj.Type != NetworkEventType.Nothing)
            {
                if (obj.Type != NetworkEventType.Data)
                {
                    Console.WriteLine("ServerEvent: " + obj.Type);
                }

                if (obj.Type == NetworkEventType.Connect)
                {
                    clientId = obj.Connection.Id;
                    clientConnection = obj.Connection;
                    Console.WriteLine("ID Client: " + obj.Connection.Id + "; Connection: " + obj.Connection);
                    //Add client to list
                    //https://www.dotnetperls.com/keyvaluepair
                    clients.Add(new KeyValuePair<ulong, Connection>(obj.Connection.Id, obj.Connection));
                }

                if (obj.Type == NetworkEventType.Data)
                {
                    messagesReceived++;
                    Console.WriteLine("Got message: \"" + Encoding.ASCII.GetString(obj.Data.Array, obj.Data.Offset, obj.Data.Count) + "\"");
                    Console.WriteLine("Offset: " + obj.Data.Offset + ", Count: " + obj.Data.Count);
                    PrintByteArray(SliceMe(obj.Data.Array, obj.Data.Count));
                }

                if (obj.Type == NetworkEventType.Disconnect)
                {
                    //Disconnected Client
                    Console.WriteLine("ServerEvent enter: " + obj.Type);
                    //Remove client from list
                    clients.RemoveAll(item => item.Value.Equals(obj.Connection));
                }

                if (obj.Type == NetworkEventType.Timeout)
                {
                    //Timeouted Client
                    Console.WriteLine("ServerEvent enter: " + obj.Type);
                    //Remove client from list
                    clients.RemoveAll(item => item.Value.Equals(obj.Connection));
                }

                obj.Recycle();
            }

        }

        //https://stackoverflow.com/a/406576
        /// <summary>
        /// Slice array
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] SliceMe(byte[] source, int length)
        {
            byte[] destfoo = new byte[length];
            //Array
            Array.Copy(source, 0, destfoo, 0, length);
            return destfoo;
        }

        /// <summary>
        /// Return path of main assembly
        /// </summary>
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        //https://stackoverflow.com/questions/10940883/c-converting-byte-array-to-string-and-printing-out-to-console
        /// <summary>
        /// Prints the byte array.
        /// </summary>
        /// <param name="bytes">Bytes.</param>
        public static void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }

        //https://stackoverflow.com/posts/9543797/revisions
        //https://stackoverflow.com/questions/9543715/generating-human-readable-usable-short-but-unique-ids?answertab=votes#tab-top
        /// <summary>
        /// Random identifier generator.
        /// </summary>
        public static class RandomIdGenerator
        {
            private static char[] _base62chars =
                "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
                .ToCharArray();

            private static Random _random = new Random();

            public static string GetBase62(int length)
            {
                var sb = new StringBuilder(length);

                for (int i = 0; i < length; i++)
                    sb.Append(_base62chars[_random.Next(62)]);

                return sb.ToString();
            }

            public static string GetBase36(int length)
            {
                var sb = new StringBuilder(length);

                for (int i = 0; i < length; i++)
                    sb.Append(_base62chars[_random.Next(36)]);

                return sb.ToString();
            }
        }
    }
}