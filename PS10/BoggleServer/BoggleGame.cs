﻿// Authors: Blake Burton, Cameron Minkel
// Start date: 11/20/14

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking;
using System.Threading;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace BB
{
    /// <summary>
    /// This class represents a game of Boggle.
    /// It requires two Players with which the
    /// server has already established connections.
    /// The game will use the settings specified
    /// at the command line when the program is launched.
    /// </summary>
    internal class BoggleGame
    {
        private Player one;
        private Player two;
        private Timer timer; // Game Timer
        private BoggleBoard board; // The board layout of the current game.
        private int timeLeft;
        private bool gameDone; // Changes the state of the game when timeleft reaches zero.
        private bool gameStart; // Is true when game starts, false otherwise.
        private readonly object playerlock; // Lock that protects Player while calculating scores.


        /// <summary>
        /// Initializes a Boggle game with the specified Players.
        /// </summary>
        /// <param name="one">Player one</param>
        /// <param name="two">Player two</param>
        public BoggleGame(Player one, Player two)
        {
            // Store the players.
            this.one = one;
            this.two = two;

            gameStart = false;

            // Begin waiting for messages from the Players.
            one.Ss.BeginReceive(WordReceived, one);
            two.Ss.BeginReceive(WordReceived, two);

            // Get a Timer ready for the gameplay countdown. It will be started later.
            timer = new Timer(TimeUpdate, null, Timeout.Infinite, Timeout.Infinite);
            timeLeft = BoggleServer.GameLength;
            gameDone = false;

            // Create a BoggleBoard with the specified
            // string of letters.  Random otherwise.
            if (BoggleServer.CustomBoard == null)
                board = new BoggleBoard();
            else
                board = new BoggleBoard(BoggleServer.CustomBoard);

            // Lock
            playerlock = new object();
        }


        /// <summary>
        /// Starts this BoggleGame.
        /// </summary>
        public void Start()
        {
            // Let the Players know the game has started.
            one.Ss.BeginSend("START " + board.ToString() + " "
                + timeLeft + " " + two.Name + "\n", SendCallback, one);
            two.Ss.BeginSend("START " + board.ToString() + " "
                + timeLeft + " " + one.Name + "\n", SendCallback, two);

            // Starts the timer. The method TimeUpdate will
            // be called every second. Start delayed by 250ms.
            timer.Change(250, 1000);
            gameStart = true;
        }


        /// <summary>
        /// Called when a message has been sent through the StringSocket
        /// to a Player. Exceptions will end this BoggleGame.
        /// </summary>
        /// <param name="e">an Exception, if any</param>
        /// <param name="payload">the StringSocket connecting
        /// the server and Player</param>
        private void SendCallback(Exception e, object payload)
        {
            if (e != null)
            {
                Terminate(e, payload);
            }
        }


        /// <summary>
        /// Called when a word has been received through the
        /// StringSocket with a Player. Exceptions will end this
        /// BoggleGame. If the received string doesn't begin with
        /// "WORD ", the message is ignored. Words will be scored
        /// and updated as the game progresses.
        /// </summary>
        /// <param name="s">the received string</param>
        /// <param name="e">an Exception, if any</param>
        /// <param name="payload">the Player from which the message
        /// was received</param>
        private void WordReceived(string s, Exception e, object payload)
        {

            // If e and s are null, end game.
            if (s == null)
            {
                Terminate(e, payload);
                return;
            }

            // Saves player for clarity purposes.
            Player player = (Player)payload;

            // Only listen for more words if game is still going.
            if (!gameDone)
                player.Ss.BeginReceive(WordReceived, player);

            if (!gameStart)
            {
                player.Ss.BeginSend("IGNORING " + s + "\n", SendCallback, player);
                return;
            }

            // If the word was received with the required preceeding
            // text, store it. Otherwise ignore what was received.
            string word;
            if (Regex.IsMatch(s.ToUpper(), @"^(WORD\s)"))
            {
                word = s.Substring(5).Trim().ToUpper();
            }
            else
            {
                player.Ss.BeginSend("IGNORING " + s + "\n", SendCallback, player);
                return;
            }

            // Words must be atleast 3 characters long.
            if (word.Length < 3)
                return;

            // Ensure the Players' data is thread safe.
            lock (playerlock)
            {
                // Do nothing if the word was already played.
                if (player.LegalWords.Contains(word) || player.IllegalWords.Contains(word))
                    return;

                // If a received legal word was already played by 
                // the opponent, the points cancel out each other
                // and the word will not appear in either Player's 
                // list of legal words played.
                if (player.Opponent.LegalWords.Contains(word))
                {
                    player.Opponent.LegalWords.Remove(word);
                    player.Opponent.Score -= WordValue(word); //subtract points from opponent.

                    // Keep track of the shared words, and update
                    // the scores.
                    one.SharedLegalWords.Add(word);
                    UpdateScore();
                    return;
                }

                // We have a potential point earning word if we make it here. Award
                // the appropriate points if it's legal. Subtract a point otherwise.
                if (BoggleServer.LegalWords.Contains(word) && board.CanBeFormed(word))
                {
                    player.LegalWords.Add(word);
                    player.Score += WordValue(word);
                    UpdateScore();
                }
                else
                {
                    player.IllegalWords.Add(word);
                    player.Score--;
                    UpdateScore();
                }
            } // end lock
        } // end private method WordReceived


        /// <summary>
        /// Sends to each Player both their score and their opponent's.
        /// </summary>
        private void UpdateScore()
        {
            one.Ss.BeginSend("SCORE " + one.Score + " " + two.Score + "\n",
                SendCallback, one);
            two.Ss.BeginSend("SCORE " + two.Score + " " + one.Score + "\n",
                SendCallback, two);
        }


        /// <summary>
        /// Returns the score value of the specified word.
        /// 3 or 4 length is worth 1 point.
        /// 5 is worth 2 points.
        /// 6 is worth 3 points.
        /// 7 is worth 5 points.
        /// 8+ is worth 11 points.
        /// </summary>
        /// <param name="word">the word</param>
        /// <returns>the word's value</returns>
        private int WordValue(string word)
        {
            int points = 0;

            switch (word.Length)
            {
                case 3:
                case 4:
                    points = 1;
                    break;
                case 5:
                    points = 2;
                    break;
                case 6:
                    points = 3;
                    break;
                case 7:
                    points = 5;
                    break;
                default:
                    points = 11;
                    break;
            }

            return points;
        }


        /// <summary>
        /// Called by the Timer every second. The time left
        /// in the game is decremented by one and the Player's
        /// are sent said value each time this method is called.
        /// The game will end once the time runs out.
        /// </summary>
        /// <param name="stateInfo">NOT USED</param>
        private void TimeUpdate(object stateInfo)
        {
            // Send both Players the remaining time.
            timeLeft--;
            one.Ss.BeginSend("TIME " + timeLeft + "\n", SendCallback, one);
            two.Ss.BeginSend("TIME " + timeLeft + "\n", SendCallback, two);

            // End the game if time is out.
            if (timeLeft == 0)
            {
                gameDone = true;
                timer.Dispose();
                End();
            }
        }


        /// <summary>
        /// Called when an Exception occurs during communication
        /// with a Player. This BoggleGame will terminate and the
        /// remaining Player will be notified.
        /// </summary>
        /// <param name="e">the Exception that occured</param>
        /// <param name="payload">the Player with which the
        /// exception occured</param>
        private void Terminate(Exception e, object payload)
        {
            // Close socket to offending player
            Player dead = (Player)payload;
            CloseSocket(null, payload);

            // Notify then close socket to remaining Player
            if (dead.Opponent.Ss.Connected)
                dead.Opponent.Ss.BeginSend("TERMINATED\n", CloseSocket, dead.Opponent);

        }


        /// <summary>
        /// Closes the socket if not already done so.
        /// </summary>
        /// <param name="e">NOT USED</param>
        /// <param name="payload">Player Stringsocket to close.</param>
        private void CloseSocket(Exception e, object payload)
        {
            // Close the StringSocket to the Player.
            ((Player)payload).Ss.Close();
        }


        /// <summary>
        /// Ends this BoggleGame. Sends the final score and 
        /// game summary messages to each Player. 
        /// Then the StringSockets are closed to each Player.
        /// </summary>
        private void End()
        {
            // Wait 1 second just to make sure everything is finished
            Thread.Sleep(1000);

            // Convert each list of words into a single string
            // (list size) followed by words seperated by spaces.
            string playerOneLegal = SetToString(one.LegalWords);
            string playerTwoLegal = SetToString(two.LegalWords);
            string shareLegal = SetToString(one.SharedLegalWords);
            string playerOneIllegal = SetToString(one.IllegalWords);
            string playerTwoIllegal = SetToString(two.IllegalWords);

            // Use the above strings to create messages to send to
            // each Player.
            string playerOneStats = "STOP" + playerOneLegal + playerTwoLegal
                + shareLegal + playerOneIllegal + playerTwoIllegal + "\n";
            string playerTwoStats = "STOP" + playerTwoLegal + playerOneLegal
                + shareLegal + playerTwoIllegal + playerOneIllegal + "\n";

            // Send the messages
            one.Ss.BeginSend(playerOneStats, CloseSocket, one);
            two.Ss.BeginSend(playerTwoStats, CloseSocket, two);

            // THE BELOW WAS USED FOR THE DATABASE
            //UpdateDatabase();

        } // end private method End


        // THE BELOW WAS USED FOR THE DATABASE
        ///// <summary>
        ///// Records information about this BoggleGame
        ///// after the game has completed in the database
        ///// specified in connectionString.
        ///// </summary>
        //private void UpdateDatabase()
        //{

        //    // Get this game's unique ID.
        //    int gameId = ++BoggleServer.GameId;

        //    // Create a connection to the database specified in connectionString.
        //    using (MySqlConnection conn = new MySqlConnection(BoggleServer.connectionString))
        //    {
        //        conn.Open();
        //        MySqlCommand command = conn.CreateCommand();

        //        // Create command to insert player 1's name into the Players table.
        //        command.CommandText = "INSERT INTO Players(player_name) " +
        //            "VALUES (@player1_name)";
        //        command.Prepare();               
        //        command.Parameters.AddWithValue("player1_name", one.Name);

        //        // Execute the above command.
        //        try
        //        {
        //            command.ExecuteNonQuery();
        //        }
        //        catch (MySqlException e) { }

        //        // Create command to insert player 2's name into the Players table.
        //        command.CommandText = "INSERT INTO Players(player_name) " +
        //            "VALUES (@player2_name)";
        //        command.Prepare();
        //        command.Parameters.AddWithValue("player2_name", two.Name);

        //        // Execute the above command.
        //        try
        //        {
        //            command.ExecuteNonQuery();
        //        }
        //        catch (MySqlException e) { }

        //        // Create command to select the rows of player 1 and player 2.
        //        command.CommandText = "SELECT * FROM Players WHERE player_name='" + one.Name +
        //            "' OR player_name='" + two.Name + "'";

        //        // Execute above command and get the IDs of player 1 and player 2.
        //        using (MySqlDataReader reader = command.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                if ((string)reader["player_name"] == one.Name)
        //                    one.Id = (int)reader["player_id"];
        //                else
        //                    two.Id = (int)reader["player_id"];
        //            }
        //        }

        //        // Create command to insert game information into the Games table.
        //        command.CommandText = "INSERT INTO Games(player_1_id, player_1_score, player_2_id, " +
        //            "player_2_score, board_config, time_limit) VALUES (@p1id, @p1s, @p2id, @p2s, @board, @time)";
        //        command.Prepare();
        //        command.Parameters.AddWithValue("p1id", one.Id);
        //        command.Parameters.AddWithValue("p1s", one.Score);
        //        command.Parameters.AddWithValue("p2id", two.Id);
        //        command.Parameters.AddWithValue("p2s", two.Score);
        //        command.Parameters.AddWithValue("board", board.ToString());
        //        command.Parameters.AddWithValue("time", BoggleServer.GameLength.ToString());

        //        // Execute the above command.
        //        command.ExecuteNonQuery();

        //        // Insert each legal word from player 1 into the Words table.
        //        foreach (string word in one.LegalWords)
        //        {
        //            command.CommandText = "INSERT INTO Words(word, game_id, player_id, word_type) " +
        //                "VALUES (@word, @gameid, @playerid, @type)";
        //            command.Prepare();
        //            command.Parameters.AddWithValue("word", word);
        //            command.Parameters.AddWithValue("gameid", gameId);
        //            command.Parameters.AddWithValue("playerid", one.Id);
        //            command.Parameters.AddWithValue("type", 0);
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();

        //        }

        //        // Insert each legal word from player 2 into the Words table.
        //        foreach (string word in two.LegalWords)
        //        {
        //            command.CommandText = "INSERT INTO Words(word, game_id, player_id, word_type) " +
        //                "VALUES (@word, @gameid, @playerid, @type)";
        //            command.Prepare();
        //            command.Parameters.AddWithValue("word", word);
        //            command.Parameters.AddWithValue("gameid", gameId);
        //            command.Parameters.AddWithValue("playerid", two.Id);
        //            command.Parameters.AddWithValue("type", 0);
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();

        //        }

        //        // Insert each illegal word from player 1 into the Words table.
        //        foreach (string word in one.IllegalWords)
        //        {
        //            command.CommandText = "INSERT INTO Words(word, game_id, player_id, word_type) " +
        //                "VALUES (@word, @gameid, @playerid, @type)";
        //            command.Prepare();
        //            command.Parameters.AddWithValue("word", word);
        //            command.Parameters.AddWithValue("gameid", gameId);
        //            command.Parameters.AddWithValue("playerid", one.Id);
        //            command.Parameters.AddWithValue("type", 1);
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();

        //        }

        //        // Insert each illegal word from player 2 into the Words table.
        //        foreach (string word in two.IllegalWords)
        //        {
        //            command.CommandText = "INSERT INTO Words(word, game_id, player_id, word_type) " +
        //                "VALUES (@word, @gameid, @playerid, @type)";
        //            command.Prepare();
        //            command.Parameters.AddWithValue("word", word);
        //            command.Parameters.AddWithValue("gameid", gameId);
        //            command.Parameters.AddWithValue("playerid", two.Id);
        //            command.Parameters.AddWithValue("type", 1);
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();

        //        }

        //        // Insert each shared word from player 1 into the Words table.
        //        foreach (string word in one.SharedLegalWords)
        //        {
        //            command.CommandText = "INSERT INTO Words(word, game_id, player_id, word_type) " +
        //                "VALUES (@word, @gameid, @playerid, @type)";
        //            command.Prepare();
        //            command.Parameters.AddWithValue("word", word);
        //            command.Parameters.AddWithValue("gameid", gameId);
        //            command.Parameters.AddWithValue("playerid", one.Id);
        //            command.Parameters.AddWithValue("type", 2);
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();

        //        }

        //        // Insert each shared word from player 2 into the Words table.
        //        foreach (string word in one.SharedLegalWords)
        //        {
        //            command.CommandText = "INSERT INTO Words(word, game_id, player_id, word_type) " +
        //                "VALUES (@word, @gameid, @playerid, @type)";
        //            command.Prepare();
        //            command.Parameters.AddWithValue("word", word);
        //            command.Parameters.AddWithValue("gameid", gameId);
        //            command.Parameters.AddWithValue("playerid", two.Id);
        //            command.Parameters.AddWithValue("type", 2);
        //            command.ExecuteNonQuery();
        //            command.Parameters.Clear();

        //        }
        //    }
        //} // end private UpdateDatabase


        /// <summary>
        /// Uses the specified HashSet of words to
        /// create a summary string for those words
        /// starting with the size of the Hashset.
        /// </summary>
        /// <param name="set">the set of words</param>
        /// <returns>a summary string</returns>
        private string SetToString(HashSet<string> set)
        {
            string temp = " " + set.Count;
            foreach (string s in set)
            {
                temp += " " + s;
            }
            return temp;
        }

    } // end class BoggleGame
} // end namespace BB
