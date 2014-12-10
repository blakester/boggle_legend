// Authors: Blake Burton, Cameron Minkel
// Start date: 11/18/14

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CustomNetworking;
using System.Threading;
using MySql.Data.MySqlClient;

namespace BB
{
    /// <summary>
    /// This class contains the main method to launch a Boggle server.
    /// The server takes 3 command line arguments: the length of the game
    /// in seconds, the pathname to the file of the legal words to use in
    /// the game, and an optional 16 character string specifying the
    /// 16 letters to be used in the board.
    /// 
    /// Once two clients have connected to this server on port 2000, the
    /// server will start a Boggle game between the two.
    /// </summary>
    public class BoggleServer
    {
        private TcpListener server; // Used to listen for player connections.
        private Player firstPlayer = null; // Used to hold the first player to connect.
        private readonly object playerMatch = new object(); // Lock for firstPlayer.
        public static string connectionString = "server=atr.eng.utah.edu;database=cs3500_blakeb;" +
            "uid=cs3500_blakeb;password=249827684";

        /// <summary>
        /// The length of a game in seconds.
        /// Must be > 0. Will be -1 if server fails to load.
        /// </summary>
        public static int GameLength
        { get; private set; }


        /// <summary>
        /// The list of legal words for a game.
        /// </summary>
        public static HashSet<string> LegalWords
        { get; private set; }


        /// <summary>
        /// An optional string of letters specifiying
        /// the board layout for a game.
        /// </summary>
        public static string CustomBoard
        { get; private set; }


        /// <summary>
        /// .
        /// </summary>
        public static int GameId
        { 
            get
            {
                lock(new object())
                {
                    return GameId++;
                }
            }
            private set{}
        }


        /// <summary>
        /// Starts the server to begin listening for connections. Boggle
        /// games will use the settings specified in the command line
        /// arguments.
        /// </summary>
        /// <param name="args">String array consisting of: length of
        /// game in seconds, pathname to legal words file, and 16 character
        /// string specifying the board layout.</param>
        public static void Main(string[] args)
        {
            // Initilize server
            BoggleServer BS = new BoggleServer(args);
            Console.Read(); //Keep Console window open.
         
            // Closes TCPListener.
            if (BS.server != null)
                BS.CloseServer();
        }


        /// <summary>
        /// Validates the command line arguments and begins listening
        /// for client connections.
        /// </summary>
        /// <param name="args">String array consisting of: length of
        /// game in seconds, pathname to legal words file, and 16 character
        /// string specifying the board layout</param>
        public BoggleServer(string[] args)
        {
            // There must be exactly two or three non-null arguments.
            if (args == null || args.Length < 2 || args.Length > 3)
            {
                // Error Message
                Console.WriteLine("Must pass in exactly two or three non-null argurments. ");
                Console.WriteLine("Press Enter to exit.");
                GameLength = -1; // Used for testing purposes.
                return;
            }

            // Check validity of game length.
            int temp = -1;
            if (!(int.TryParse(args[0], out temp)) || temp < 1)
            {
                // Error Message
                Console.WriteLine("First argument must be a positive integer. ");
                Console.WriteLine("Press Enter to exit.");
                GameLength = -1; // Used for testing purposes.
                return;
            }
            GameLength = temp;

            // Check validity of the file path. If valid, the legal 
            // words are added to a HashSet. Null otherwise.
            try
            {
                LegalWords = new HashSet<string>(
                    System.IO.File.ReadAllLines(args[1]));
            }
            catch (Exception e)
            {
                // Error Message
                Console.Write("EXCEPTION: " + e.Message);
                Console.WriteLine("Press Enter to exit.");
                GameLength = -1; // Used for testing purposes.
                return;
            }

            // Check validity of optional Boggle Board layout string.
            if (args.Length == 3)
            {
                if (!(Regex.IsMatch(args[2].ToUpper(), @"^[A-Z]{16}$")))
                {
                    // Error Message
                    Console.WriteLine("Third argument must be exactly 16 letters or empty.");
                    Console.WriteLine("Press Enter to exit.");
                    GameLength = -1; // Used for testing purposes.
                    return;
                }
                CustomBoard = args[2];
            }
            else
                CustomBoard = null;

            using(MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                MySqlCommand command = conn.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Games";

                using(MySqlDataReader reader = command.ExecuteReader())
                {
                    GameId = (int)command.ExecuteScalar();
                }
            }

            // Begin listening for connections on port 2000
            server = new TcpListener(IPAddress.Any, 2000);
            server.Start();
            server.BeginAcceptSocket(ConnectionRequested, null);

        }// end constructor


        /// <summary>
        /// Called when a connection has been received.
        /// </summary>
        /// <param name="result">Result of BeginAcceptSocket</param>
        private void ConnectionRequested(IAsyncResult result)
        {
            // Create a StringSocket and begin listening for
            // the "PLAY" command from the client.
            Socket s = server.EndAcceptSocket(result);
            StringSocket ss = new StringSocket(s, Encoding.UTF8);
            ss.BeginReceive(Play, ss); // Send StringSocket to be paired up.

            // Begin listening for another connection.
            server.BeginAcceptSocket(ConnectionRequested, null);
        }


        /// <summary>
        /// Called when a message has been received through
        /// the StringSocket from a client. A new game will begin
        /// when two players have sent the command "PLAY ". Will 
        /// ignore if string does not start as such. If 'e' is 
        /// non-null, then we assume that the connection has been 
        /// lost.
        /// </summary>
        /// <param name="s">the received string</param>
        /// <param name="e">an Exception, if any</param>
        /// <param name="payload">the StringSocket connecting
        /// the server and client</param>
        private void Play(String s, Exception e, object payload)
        {
            // If e is non null, set up game.  Otherwise close
            // socket.
            if (e == null && s != null)
            {
                // To begin play, the command must start
                // exactly with "PLAY ". Ignore otherwise.
                if (Regex.IsMatch(s.ToUpper(), @"^(PLAY\s)"))
                {
                    // Create a new Player using the player's name
                    // and StringSocket connection with the server.
                    string name = s.Substring(5);
                    Player thisPlayer = new Player(name.Trim(), (StringSocket)payload);

                    // Keep firstPlayer threadsafe.
                    lock (playerMatch)
                    {
                        // Null if first player, non-null if second player.
                        if (firstPlayer == null)
                            firstPlayer = thisPlayer; 
      
                        // We have two players, so start a game between them
                        // in it's own thread so the lock can be released asap.
                        else
                        {
                            firstPlayer.Opponent = thisPlayer; // remembers opponent
                            thisPlayer.Opponent = firstPlayer;
                            BoggleGame game = new BoggleGame(firstPlayer, thisPlayer);
                            game.Start();
                            firstPlayer = null; // gets firstPlayer ready for next pair up.

                        }// end else
                    }// end Lock
                }// end if
                else
                {

                    StringSocket temp = (StringSocket)payload;
                    temp.BeginSend("IGNORING " + s + "\n", MessageSent, null);
                    temp.BeginReceive(Play, temp);

                }// end else
            }// end if
            else
            {
                // If offending socket is firstPlayer, remove firstPlayer
                ((StringSocket)payload).Close(); //Close offending socket

            }// end else
        } // end method Play


        /// <summary>
        /// Callback for when a message has been sent through
        /// the StringSocket to a client. Non-null Exceptions 
        /// will close the connection with the client.
        /// </summary>
        /// <param name="e">an Exception, if any</param>
        /// <param name="payload">the StringSocket connecting
        /// the server and client</param>
        private void MessageSent(Exception e, object payload)
        {
            if (e != null)
            {
                ((StringSocket)payload).Close();
            }
        }

        /// <summary>
        /// Stops Server.
        /// </summary>
        public void CloseServer()
        {
            server.Stop();
        }
    } // end class BoggleServer
} // end namespace BB
