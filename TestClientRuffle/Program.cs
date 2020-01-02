//Ruffles
using Ruffles.Configuration;
using Ruffles.Connections;
using Ruffles.Core;

//System
using System;
using System.Net;
using System.Text;

namespace TestClientRuffle
{
    class Program
    {
        /// <summary>
        /// Initial Config
        /// </summary>
        internal static readonly SocketConfig ClientConfig = new SocketConfig()
        {
            ChallengeDifficulty = 20, // Difficulty 20 is fairly hard
            DualListenPort = 0, // Port 0 means we get a port by the operating system
            SimulatorConfig = new Ruffles.Simulation.SimulatorConfig()
            {
                DropPercentage = 0.05f,
                MaxLatency = 10,
                MinLatency = 0
            },
            UseSimulator = false
        };

        static RuffleSocket client = new RuffleSocket(ClientConfig);
        // The client stores the servers id here
        static ulong serverId = 0;
        static Connection serverConnection = null;

        // The time when the connection started
        DateTime started = DateTime.Now;

        // The time when the last message was sent
        static DateTime lastSent = DateTime.MinValue;

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

            try
            {
                //Star connection
                client.OnNetworkEvent += Client_OnNetworkEvent;
                client.Start();
                Console.WriteLine("Connecting...");
                {
                    // IPv4 Connect
                    client.ConnectNow(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5674));
                }

                //Start Thread
                ConsoleKeyInfo cki;
                //Prevent example from ending if CTL+C is pressed.
                Console.TreatControlCAsInput = true;

                Console.WriteLine("Press any combination of CTL, ALT, and SHIFT, and a console key.");
                Console.WriteLine("Press the Escape (Esc) key to quit: \n");
                do
                {
                    cki = Console.ReadKey();
                    Console.Write(" --- You pressed ");
                    if ((cki.Modifiers & ConsoleModifiers.Alt) != 0) Console.Write("ALT+");
                    if ((cki.Modifiers & ConsoleModifiers.Shift) != 0) Console.Write("SHIFT+");
                    if ((cki.Modifiers & ConsoleModifiers.Control) != 0) Console.Write("CTL+");
                    //Send test message
                    if (cki.Key.ToString().ToLower() == "s")
                    {
                        //Send test message
                        byte[] helloReliable = Encoding.ASCII.GetBytes("This message was sent over a reliable channel " + messageCounter);
                        client.SendNow(new ArraySegment<byte>(helloReliable, 0, helloReliable.Length), serverId, 1, false);
                        Console.WriteLine("Sending packet: " + messageCounter);

                        messageCounter++;
                        lastSent = DateTime.Now;
                    }
                    Console.WriteLine(cki.Key.ToString());
                } while (cki.Key != ConsoleKey.Escape);

                client.Stop();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message + " from " + ex.Source);
                client.Shutdown();
                Environment.Exit(1);
            }

        }

        /// <summary>
        /// Receive NetworkEvent 
        /// </summary>
        /// <param name="obj"></param>
        private static void Client_OnNetworkEvent(NetworkEvent obj)
        {
            if (obj.Type != NetworkEventType.Nothing)
            {
                if (obj.Type != NetworkEventType.Data)
                {
                    Console.WriteLine("ClientEvent: " + obj.Type);
                }

                if (obj.Type == NetworkEventType.Connect)
                {
                    serverId = obj.Connection.Id;
                    serverConnection = obj.Connection;
                }

                if (obj.Type == NetworkEventType.Data)
                {
                    messagesReceived++;
                    Console.WriteLine("Got message: \"" + Encoding.ASCII.GetString(obj.Data.Array, obj.Data.Offset, obj.Data.Count) + "\"");
                }
            }

            obj.Recycle();
        }
    }
}

