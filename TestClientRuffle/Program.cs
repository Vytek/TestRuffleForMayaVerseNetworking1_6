using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Ruffles
using Ruffles.Channeling;
using Ruffles.Configuration;
using Ruffles.Simulation;
using Ruffles.Connections;
using Ruffles.Core;

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

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            // The client stores the servers id here
            ulong serverId = 0;
            Connection serverConnection = null;

            // The time when the connection started
            DateTime started = DateTime.Now;

            // The time when the last message was sent
            DateTime lastSent = DateTime.MinValue;

            // The time the last status was printed
            DateTime lastStatusPrint = DateTime.MinValue;

            // The amount of message that has been received
            int messagesReceived = 0;

            // The amount of messages that has been sent
            int messageCounter = 0;

            try
            {
                //Star connection
                client.Start();

                {
                    // IPv4 Connect
                    client.ConnectNow(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5674));
                }
            }
            catch (Exception ex)
            {

            }

        }
    }
}
