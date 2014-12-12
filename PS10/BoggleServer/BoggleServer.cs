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
        private TcpListener webServer;
        private Player firstPlayer = null; // Used to hold the first player to connect.
        private readonly object playerMatch = new object(); // Lock for firstPlayer.
        public static string connectionString = "server=atr.eng.utah.edu;database=cs3500_blakeb;" +
            "uid=cs3500_blakeb;password=249827684";
        public static int gameId;

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
        //public static int GameId
        //{ 
        //    get
        //    {
        //        lock(new object())
        //        {
        //            return gameId++;
        //        }
        //    }
        //    private set { gameId = value; }
        //}


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

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                MySqlCommand command = conn.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Games";

                gameId = Convert.ToInt32(command.ExecuteScalar());
            }

            // Begin listening for connections on port 2000
            server = new TcpListener(IPAddress.Any, 2000);
            server.Start();
            webServer = new TcpListener(IPAddress.Any, 2500);
            webServer.Start();
            server.BeginAcceptSocket(ConnectionRequested, null);
            webServer.BeginAcceptSocket(WebRequested, null);

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

        private void WebRequested(IAsyncResult result)
        {
            Socket s = webServer.EndAcceptSocket(result);
            StringSocket ss = new StringSocket(s, Encoding.UTF8);
            ss.BeginReceive(SendPage, ss);

            webServer.BeginAcceptSocket(WebRequested, null);
        }


        private void SendPage(string request, Exception e, object payload)
        {
            if (e != null || request == null)
            {
                ((StringSocket)payload).Close();
                return;
            }



            //StringSocket ss = (StringSocket)payload;

            //ss.BeginSend("HTTP/1.1 200 OK\r\n" +
            //"Connection: close\r\n" +
            //"Content-Type: text/html; charset=UTF-8\r\n" +
            //"\r\n" +
            //"<!DOCTYPE html><html><body><h1>My First Heading</h1><p>My first paragraph.</p></body></html>", (ex, o) => { }, null);

            //ss.Close();

            string stringPattern1 = @"^(GET /players)";
            string stringPattern2 = @"^(GET /games\?player=)";
            string stringPattern3 = @"^(GET /game\?id=)";


            if (Regex.IsMatch(request, stringPattern1))
            {
                MainPage(payload);
            }
            else if (Regex.IsMatch(request, stringPattern2))
            {
                string temp = request.Substring(18);
                temp = temp.Remove(temp.Length - 10);
                PlayerPage(temp, payload);

            }
            else if (Regex.IsMatch(request, stringPattern3))
            {
                string temp = request.Substring(13);
                temp = temp.Remove(temp.Length - 10);
                GamePage(int.Parse(temp), payload);
            }
            else
            {
                ErrorPage();
            }

        }

        private void MainPage(object payload)
        {
            string page = "HTTP/1.1 200 OK\r\n" +
            "Connection: close\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "\r\n" +
            "<!DOCTYPE html><html>" +
            "<style>" +
                "table { width:500px; }" +
                "table, th, td { border: 1px solid black; border-collapse: collapse; }" +
                "th, td { padding: 5px; text-align: left; }" +
                "table#t01 tr:nth-child(even) { background-color: #eee; }" +
                "table#t01 tr:nth-child(odd) { background-color: #fff; }" +
                "table#t01 th { background-color: black; color: white; }" +
            "</style>" +
            "<body><h1>Player Standings</h1><table id='t01'>" +
            "<tr><th>Player Name</th><th>Wins</th><th>Losses</th><th>Ties</th></tr>";

            StringSocket ss = (StringSocket)payload;
            Dictionary<int ,string> players = new Dictionary<int,string>();
            


            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                MySqlCommand command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM Players";

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        players.Add((int)reader["player_id"], (string)reader["player_name"]);
                    }
                }

                foreach(KeyValuePair<int, string> player in players)
                {
                    int win = 0;
                    int loss = 0;
                    int tie = 0;
                    command.Parameters.Clear();
                    command.CommandText = "SELECT * FROM Games WHERE player_1_id = @id";
                    command.Prepare();
                    command.Parameters.AddWithValue("id", player.Key);
                    using (MySqlDataReader reader2 = command.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            if ((int)reader2["player_1_score"] > (int)reader2["player_2_score"])
                                win++;
                            else if ((int)reader2["player_1_score"] < (int)reader2["player_2_score"])
                                loss++;
                            else
                                tie++;
                        }
                    }

                    command.Parameters.Clear();
                    command.CommandText = "SELECT * FROM Games WHERE player_2_id = @id";
                    command.Prepare();
                    command.Parameters.AddWithValue("id", player.Key);
                    using (MySqlDataReader reader2 = command.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            if ((int)reader2["player_2_score"] > (int)reader2["player_1_score"])
                                win++;
                            else if ((int)reader2["player_2_score"] < (int)reader2["player_1_score"])
                                loss++;
                            else
                                tie++;
                        }
                    }

                    page += "<tr><td>" + player.Value + "</td><td>" + win + "</td><td>" + loss + "</td><td>" + tie + "</td></tr>";
                    //page += name + ": Wins (" + win + ") Losses (" + loss + ") Ties (" + tie + ")<br>";
                }

            }

            page += "</p></body></html>";
            ss.BeginSend(page, (e, x) => { ss.Close(); }, null);
        }

        private void PlayerPage(String player, object payload)
        {
            string page = "HTTP/1.1 200 OK\r\n" +
            "Connection: close\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "\r\n" +
            "<!DOCTYPE html><html>" +
            "<style>" +
                "table { width:500px; }" +
                "table, th, td { border: 1px solid black; border-collapse: collapse; }" +
                "th, td { padding: 5px; text-align: left; }" +
                "table#t01 tr:nth-child(even) { background-color: #eee; }" +
                "table#t01 tr:nth-child(odd) { background-color: #fff; }" +
                "table#t01 th { background-color: black; color: white; }" +
            "</style>" +
            "<body><h1>" + player + " Stats</h1><table id='t01'>" +
            "<tr><th>Game Number</th><th>Date</th><th>Player's Score</th><th>Opponent's Name</th><th> Opponenet's Score<th></tr>";

            StringSocket ss = (StringSocket)payload;
            List<GamePlayed> gamesPlayed= new List<GamePlayed>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                MySqlCommand command = conn.CreateCommand();

                command.Parameters.Clear();
                command.CommandText = "SELECT * FROM Players NATURAL JOIN Games WHERE Players.player_name = @name "
                    + "AND (Players.player_id = Games.player_1_id OR Players.player_id = Games.player_2_id) ORDER BY game_id";
                command.Prepare();
                command.Parameters.AddWithValue("name", player);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        GamePlayed temp = new GamePlayed();
                        temp.Name = player;
                        temp.OpponName = "";
                        temp.GameId = (int)reader["game_id"];
                        temp.Date = (System.DateTime)reader["date_time"];
                        temp.Board = (string)reader["board_config"];
                        temp.Time = (int)reader["time_limit"];

                        if((int)reader["player_id"] == (int)reader["player_1_id"])
                        {
                            
                            temp.Id = (int)reader["player_id"];
                            temp.PScore = (int)reader["player_1_score"];
                            temp.OpponId = (int)reader["player_2_id"];
                            temp.OpponScore = (int)reader["player_2_score"];
                            
                        }
                        else
                        {
                            temp.Id = (int)reader["player_id"];
                            temp.PScore = (int)reader["player_2_score"];
                            temp.OpponId = (int)reader["player_1_id"];
                            temp.OpponScore = (int)reader["player_1_score"];
                        }

                        gamesPlayed.Add(temp);
                        
                    }
                }
           
                foreach(GamePlayed game in gamesPlayed)
                {
                    command.Parameters.Clear();
                    command.CommandText = "SELECT player_name FROM Players WHERE player_id = @id";
                    command.Prepare();
                    command.Parameters.AddWithValue("id", game.OpponId);

                    game.OpponName = (string)(command.ExecuteScalar());

                    page += "<tr><td>" + game.GameId + "</td><td>" + game.Date + "</td><td>" 
                        + game.PScore + "</td><td>" + game.OpponName + "</td><td>" + game.OpponScore + "</td></tr>";
                }

                page += "</p></body></html>";
                ss.BeginSend(page, (e, x) => { ss.Close(); }, null);
            }
        }

        private void GamePage(int gameId, object payload)
        {
            string page = "HTTP/1.1 200 OK\r\n" +
            "Connection: close\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "\r\n" +
            "<!DOCTYPE html><html>" +
            "<style>" +
                "table { width:500px; }" +
                "table, th, td { border: 1px solid black; border-collapse: collapse; }" +
                "th, td { padding: 5px; text-align: left; }" +
                "table#t01 tr:nth-child(even) { background-color: #eee; }" +
                "table#t01 tr:nth-child(odd) { background-color: #fff; }" +
                "table#t01 th { background-color: black; color: white; }" +
            "</style>" +
            "<body><h1>Game Summary</h1><table id='t01'>" +
            "<tr><th>Game Number</th><th>Date</th><th>Time Limit</th></tr>";

            StringSocket ss = (StringSocket)payload;
            List<GamePlayed> gamesPlayed = new List<GamePlayed>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                MySqlCommand command = conn.CreateCommand();
                GamePlayed temp = new GamePlayed();

                command.Parameters.Clear();
                command.CommandText = "SELECT * FROM Games WHERE game_id = @id";
                command.Prepare();
                command.Parameters.AddWithValue("id", gameId);
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        temp.Id = (int)reader["player_1_id"];
                        temp.OpponId = (int)reader["player_2_id"];
                        temp.GameId = gameId;
                        temp.PScore = (int)reader["player_1_score"];
                        temp.OpponScore = (int)reader["player_2_score"];
                        temp.Date = (System.DateTime)reader["date_time"];
                        temp.Board = (string)reader["board_config"];
                        temp.Time = (int)reader["time_limit"];
                    }
                }

                command.Parameters.Clear();
                command.CommandText = "SELECT * FROM Players WHERE player_id = @p1 OR player_id = @p2";
                command.Prepare();
                command.Parameters.AddWithValue("p1", temp.Id);
                command.Parameters.AddWithValue("p2", temp.OpponId);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (temp.Id == (int)reader["player_id"])
                            temp.Name = (string)reader["player_name"];
                        else
                            temp.OpponName = (string)reader["player_name"];
                    }
                }

                page += "<tr><td>" + gameId + "</td><td>" + temp.Date + "</td><td>" + temp.Time + "</td></tr>";
                page += "<br><p>" + temp.Board + "</p><br>";
                page += "<table id='t01'><tr><th>" + temp.Name + "</th><th>" + temp.OpponName + "</th></tr>";
                page += "<tr><td>" + temp.PScore + "</td><td>" + temp.OpponScore + "</td></tr>";
            }

            page += "</p></body></html>";
            ss.BeginSend(page, (e, x) => { ss.Close(); }, null);
        }

        private void ErrorPage()
        {

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
            webServer.Stop();
        }

        private class GamePlayed
        {
            public string Name
            { get; set; }

            public int Id
            { get; set; }

            public int GameId
            { get; set; }

            public int PScore
            { get; set; }

            public string OpponName
            { get; set; }

            public int OpponId
            { get; set; }

            public int OpponScore
            { get; set; }

            public System.DateTime Date
            { get; set; }

            public string Board
            { get; set; }

            public int Time
            { get; set; }

            public GamePlayed()
            {

            }

        }
    } // end class BoggleServer
} // end namespace BB
