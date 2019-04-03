using System;
using System.Diagnostics;
using Exceling.NDatabase;

namespace Exceling
{
    public class Run
    {
        /// <summary>
        /// The entry point of the program, where the program control starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Config config = new Config();
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "-f":
                        Server server1 = new Server(config.Port, config.IP);
                        server1.InitListenSocket();
                        break;
                    case "-r":
                        LaunchReadOnly();
                        break;
                    case "-d":
                        Server server3 = new Server();
                        server3.InitListenSocket();
                        break;
                    case "-c":
                        Database database = new Database();
                        database.DropTables();
                        Console.WriteLine("Tables was droped.");
                        break;
                    case "-h":
                    case "-help":
                        Helper();
                        break;
                    default:
                        Console.WriteLine("Turn first parameter for initialize server. You can turned keys: -h or -help - to see instruction of start servers modes.");
                        break;
                }
            }
            else
            {
                Server server = new Server(config.Port, config.IP);
                server.InitListenSocket();
            }
        }
        public static void Helper()
        {
            string[] commands = { "-f [time_in_minutes]", "-r", "-d", "-c", "-h or -help" };
            string[] description =
            {
                "Start server in full working cycle. After first key, second key set time to cycle for upper program. By default, it's set 5 minutes.",
                "Start reading logs from server." ,
                "Start server in default configuration settings.",
                "Start the database cleanup mode." ,
                "Helps contains 5 modes of the server that cound be used."
            };
            Console.WriteLine();
            for (int i = 0; i < commands.Length; i++) { Console.WriteLine(commands[i] + "\t - " + description[i]); }
        }
        public static void LaunchReadOnly()
        {
            LogProgram log = new LogProgram();
            log.ReadConsoleLogsDatabase();
        }
    }
}
