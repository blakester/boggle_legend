// Authors: Blake Burton, Cameron Minkel
// Start date: 11/20/14

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking; // needed for StringSocketOfficial.dll
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
// THE BELOW WAS USED FOR THE DATABASE
//using MySql.Data.MySqlClient;

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
        private Player one, two;
        private int gameID = 0;
        private byte playCount = 0;
        private byte boardSentCount = 0;
        private byte startSentCount = 0;
        private byte resumeCount = 0;
        private byte resumeSentCount = 0;        
        private byte countDown = 3;
        private byte resumeCountDown = 3;
        private string possibleWords;
        private Timer countDownTimer, gameTimer, resumeTimer;
        private Stopwatch watch;//, deleteme;//**************************************************************************************
        private int gameLength, timeLeft, maxScore;
        private bool paused = false;
        private bool gameOver = false;
        private BoggleBoard board; // The board layout of the current game.        
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

            // Create lock to protect Player data
            playerlock = new object();

            // Initialize the timers
            countDownTimer = new Timer(CountdownUpdate, null, Timeout.Infinite, Timeout.Infinite);
            gameTimer = new Timer(TimeUpdate, null, Timeout.Infinite, Timeout.Infinite);
            resumeTimer = new Timer(ResumingUpdate, null, Timeout.Infinite, Timeout.Infinite);
            watch = new Stopwatch();
            //deleteme = new Stopwatch();//**************************************************************************************************

            // Let Players know game is ready to start
            one.Ss.BeginSend("READY " + BoggleServer.GameLength + " " + two.Name + "\n", ExceptionCheck, one);
            two.Ss.BeginSend("READY " + BoggleServer.GameLength + " " + one.Name + "\n", ExceptionCheck, two);

            // Begin waiting for messages from the Players.
            one.Ss.BeginReceive(MessageReceived, one);
            two.Ss.BeginReceive(MessageReceived, two);            
        }       


        /// <summary>
        /// Called when a message has been received through the
        /// StringSocket with a Player. Exceptions will end this
        /// BoggleGame. Words will be scored and updated as the game progresses.
        /// </summary>
        /// <param name="s">the received string</param>
        /// <param name="e">an Exception, if any</param>
        /// <param name="payload">the Player from which the message
        /// was received</param>
        private void MessageReceived(string s, Exception e, object payload)
        {
            if (s != null && e == null)
            {
                // Saves player for clarity purposes.
                Player player = (Player)payload;

                // Immediately begin listening for more messages
                player.Ss.BeginReceive(MessageReceived, player);

                // Handle the message
                if (Regex.IsMatch(s.ToUpper(), @"^(WORD\s)"))
                    ProcessWord(player, s.Substring(5).Trim().ToUpper());
                else if (Regex.IsMatch(s.ToUpper(), @"^(TYPING)"))
                    NotifyOpponentTyping(player);
                else if (Regex.IsMatch(s.ToUpper(), @"^(CHAT\s)"))                
                    RelayChatMessage(player, s.Substring(5));                
                else if (Regex.IsMatch(s.ToUpper(), @"^(PLAY)"))                
                    Play();                
                else if (Regex.IsMatch(s.ToUpper(), @"^(PAUSE)"))                
                    PauseTimer(player);                
                else if (Regex.IsMatch(s.ToUpper(), @"^(RESUME)"))                
                    ResumingCountdown();                
                else if (Regex.IsMatch(s.ToUpper(), @"^(CANCEL\s)"))                
                    Cancel(s.Substring(7));
                else if (Regex.IsMatch(s.ToUpper(), @"^(LENGTH\s)"))
                    ChangeGameLength(player, s.Substring(7));            
            }
            else
                Terminate(e, payload);                       
        }


        /// <summary>
        /// Initiates the game once both players have clicked Play
        /// </summary>
        private void Play()
        {
            lock (playerlock)
            {
                playCount++;
                if (playCount == 2)
                {
                    playCount = 0;
                    InitiateGame();
                }
            }
        }


        /// <summary>
        /// Cancels a Resume or Play request
        /// </summary>
        /// <param name="s"></param>
        private void Cancel(string s)
        {
            if (s == "True")
                resumeCount = 0;
            else
                playCount = 0;
        }


        /// <summary>
        /// Sends both players the boggle board and game length
        /// </summary>
        private void InitiateGame()
        {
            // Create a BoggleBoard with the specified
            // string of letters.  Random otherwise.
            if (BoggleServer.CustomBoard == null)
                board = new BoggleBoard();
            else
                board = new BoggleBoard(BoggleServer.CustomBoard);

            // reset timeLeft; use custom gameLength if set
            if (gameLength == 0)
                timeLeft = BoggleServer.GameLength;
            else
                timeLeft = gameLength;

            // Send players the board. Countdown starts after both messages are sent.
            one.Ss.BeginSend("BOARD " + board.ToString() + " " + timeLeft + "\n", Countdown, one);
            two.Ss.BeginSend("BOARD " + board.ToString() + " " + timeLeft + "\n", Countdown, two); 
 
            // Determine all possible words on the current board in another thread.
            // These words will be sent at the end of the game.
            ThreadPool.QueueUserWorkItem(x => { PossibleWords(); });
        }

        /// <summary>
        /// Starts countdown once both players have been sent the BOARD message.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="payload"></param>
        private void Countdown(Exception e, object payload)
        {
            if (e != null)
                Terminate(e, payload);
            else
            {
                lock (playerlock)
                {
                    boardSentCount++;
                    if (boardSentCount == 2)
                    {
                        boardSentCount = 0;

                        // though highly unlikely chronologically, it's possible 
                        // the timer could have been disposed by Terminate()
                        try { countDownTimer.Change(0, 1000); } // start the countdown
                        catch (ObjectDisposedException) { return; }
                    }
                }
            }           
        }


        /// <summary>
        /// Sends the remaining countdown time to both players each second of the countdown.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void CountdownUpdate(object stateInfo)
        {            
            if (countDown > 0)
            {              
                one.Ss.BeginSend("COUNTDOWN " + countDown + "\n", ExceptionCheck, one);
                two.Ss.BeginSend("COUNTDOWN " + countDown + "\n", ExceptionCheck, two);
                countDown--;
            }
            // countdown over
            else
            {
                // though highly unlikely chronologically, it's possible 
                // the timer could have been disposed by Terminate()
                try { countDownTimer.Change(Timeout.Infinite, Timeout.Infinite); } // freeze timer
                catch (ObjectDisposedException) { return; }

                Start(); // start the game
                countDown = 3; // reset countdown
            }
        }


        /// <summary>
        /// Starts this BoggleGame.
        /// </summary>
        private void Start()
        {
            // Let the Players know the game is starting.
            // The game won't start until both messages are sent.
            one.Ss.BeginSend("START\n", StartTimer, one);
            two.Ss.BeginSend("START\n", StartTimer, two);            
        }


        /// <summary>
        /// Starts the timer once both players have been sent the START message.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="payload"></param>
        private void StartTimer(Exception e, object payload)
        {
            if (e != null)
                Terminate(e, payload);
            else
            {
                lock (playerlock)
                {
                    startSentCount++;
                    if (startSentCount == 2)
                    {
                        startSentCount = 0;
                        gameOver = false;                       

                        // though highly unlikely chronologically, it's possible 
                        // the timer could have been disposed by Terminate()
                        try { gameTimer.Change(0, 1000); } // start timer
                        catch (ObjectDisposedException) { return; }

                        // Print start game info            
                        Console.WriteLine(string.Format("{0, -13} GAME {1, 4} {2, -15} {3, -15} {4}",
                            "START", ++gameID, one.IP, two.IP, DateTime.Now));
                    }
                }
            }
        }


        /// <summary>
        /// Pauses the game and notifies the specified player's opponent.
        /// </summary>
        /// <param name="player"></param>
        private void PauseTimer(Player player)
        {
            // Only allow one player to initiate a pause (this is only needed
            // for the highly unlikely event that both players' pauses are
            // handled at the same time. In this case, after first player passes
            // through the lock and pauses the game, the second player won't get
            // past the if statement)
            lock (playerlock)
            {
                // only allow player to pause the game if the other player hasn't already paused it
                if (!paused) 
                {
                    watch.Stop(); // get elapsed time since last time update

                    // though highly unlikely chronologically, it's possible 
                    // the timer could have been disposed by Terminate()
                    try { gameTimer.Change(Timeout.Infinite, Timeout.Infinite); } // Pause the time updates
                    catch (ObjectDisposedException) { return; }

                    paused = true;

                    // though also highly unlikely, it's possible a player could send PAUSE right before
                    // receiving the STOP message (technically, a player COULD still sneak past this)
                    if (!gameOver)
                    {
                        // notify other player of pause
                        player.Opponent.Ss.BeginSend("PAUSE\n", ExceptionCheck, player);

                        // print game paused info
                        Console.WriteLine(string.Format("{0, -13} GAME {1, 4} {2, -15} {3, -15} {4}", "PAUSE", gameID, one.IP, two.IP, DateTime.Now));
                    }
                }
            }
        }


        /// <summary>
        /// Starts the resume countdown once both players
        /// have clicked Resume.
        /// </summary>
        private void ResumingCountdown()
        {
            lock (playerlock)
            {
                resumeCount++;
                if (resumeCount == 2)
                {
                    // though highly unlikely chronologically, it's possible 
                    // the timer could have been disposed by Terminate()
                    try { resumeTimer.Change(0, 1000); } // start the resume countdown
                    catch (ObjectDisposedException) { return; }

                    resumeCount = 0;                    
                }
            }
        }


        /// <summary>
        /// Sends the remaining resuming countdown time to both players each second of the
        /// resume countdown.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void ResumingUpdate(object stateInfo)
        {
            if (resumeCountDown > 0)
            {
                one.Ss.BeginSend("RESUMING " + resumeCountDown + "\n", ExceptionCheck, one);
                two.Ss.BeginSend("RESUMING " + resumeCountDown + "\n", ExceptionCheck, two);
                resumeCountDown--;
            }
            // resuming countdown over
            else
            {
                // though highly unlikely chronologically, it's possible 
                // the timer could have been disposed by Terminate()
                try { resumeTimer.Change(Timeout.Infinite, Timeout.Infinite); } // freeze resuming timer
                catch (ObjectDisposedException) { return; }

                // Let both players know the game will resume.
                // Timer will resume once both messages are sent.
                one.Ss.BeginSend("RESUME\n", ResumeTimer, one);
                two.Ss.BeginSend("RESUME\n", ResumeTimer, two);

                resumeCountDown = 3; // reset resuming countdown
            }
        }


        /// <summary>
        /// Resumes the timer once both players have been sent
        /// the RESUME message.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="payload"></param>
        private void ResumeTimer(Exception e, object payload)
        {
            if (e != null)
                Terminate(e, payload);
            else
            {
                lock (playerlock)
                {
                    resumeSentCount++;
                    if (resumeSentCount == 2)
                    {
                        paused = false;
                        resumeSentCount = 0;
                         
                        // though highly unlikely chronologically, it's possible 
                        // the timer could have been disposed by Terminate()
                        try { gameTimer.Change(1000 - watch.ElapsedMilliseconds, 1000); } // Resume time updates once remaining time has passed
                        catch (ObjectDisposedException) { return; }

                        // print game resumed info
                        Console.WriteLine(string.Format("{0, -13} GAME {1, 4} {2, -15} {3, -15} {4}", "RESUME", gameID, one.IP, two.IP, DateTime.Now));
                    }
                }                
            }
        }


        /// <summary>
        /// Called by the Timer every second. The time left
        /// in the game is decremented by one and the Players
        /// are sent said value each time this method is called.
        /// The game will end once the time runs out.
        /// </summary>
        /// <param name="stateInfo">NOT USED</param>
        private void TimeUpdate(object stateInfo)
        {            
            if (timeLeft > 0)
            {              
                one.Ss.BeginSend("TIME " + timeLeft + "\n", ExceptionCheck, one);
                two.Ss.BeginSend("TIME " + timeLeft + "\n", ExceptionCheck, two);
                watch.Restart(); // keep track of elapsed time if someone clicks pause
                timeLeft--;
            }
            // game finished
            else
                End();
        }


        /// <summary>
        /// Sends to each Player both their score and their opponent's.
        /// </summary>
        private void UpdateScore()
        {
            one.Ss.BeginSend("SCORE " + one.Score + " " + two.Score + "\n",
                ExceptionCheck, one);
            two.Ss.BeginSend("SCORE " + two.Score + " " + one.Score + "\n",
                ExceptionCheck, two);
        }            


        /// <summary>
        /// Ends this BoggleGame. Sends the final score and 
        /// game summary messages to each Player. 
        /// </summary>
        private void End()
        {
            gameOver = true;            

            // though highly unlikely chronologically, it's possible 
            // the timer could have been disposed by Terminate()
            try { gameTimer.Change(Timeout.Infinite, Timeout.Infinite); } // freeze gameTimer
            catch (ObjectDisposedException) { return; }

            watch.Stop(); // elapsed time no longer needed

            // Convert each list of words into a single string
            // (list size) followed by words seperated by spaces.
            string playerOneLegal = SetToString(one.LegalWords);
            string playerTwoLegal = SetToString(two.LegalWords);
            string shareLegal = SetToString(one.SharedLegalWords);
            string playerOneIllegal = SetToString(one.IllegalWords);
            string playerTwoIllegal = SetToString(two.IllegalWords);

            // Use the above strings to create messages to send to each Player.
            string playerOneStats = "STOP " + maxScore + playerOneLegal + playerTwoLegal
                + shareLegal + playerOneIllegal + playerTwoIllegal + possibleWords + "\n";
            string playerTwoStats = "STOP " + maxScore + playerTwoLegal + playerOneLegal
                + shareLegal + playerTwoIllegal + playerOneIllegal + possibleWords + "\n";

            // Send the messages
            one.Ss.BeginSend(playerOneStats, ExceptionCheck, one);
            two.Ss.BeginSend(playerTwoStats, ExceptionCheck, two);

            // Clear players' data for next game
            one.Score = 0;
            one.LegalWords.Clear();
            one.IllegalWords.Clear();            
            one.SharedLegalWords.Clear();
            two.Score = 0;
            two.LegalWords.Clear();
            two.IllegalWords.Clear();            
            two.SharedLegalWords.Clear();           

            Console.WriteLine(string.Format("{0, -13} GAME {1, 4} {2, -15} {3, -15} {4}", "END", gameID, one.IP, two.IP, DateTime.Now));

            // DELETE ME!!!
            //int matches = 0;
            //foreach (string word in BoggleServer.LegalWords)
            //    if (!Regex.IsMatch(word, @"^[A-Z]+$"))
            //    {
            //        matches++;
            //        Console.WriteLine(word);
            //    }
            //Console.WriteLine(matches); // prints # of words that the if caught

            // THE BELOW WAS USED FOR THE DATABASE
            //UpdateDatabase();

        } // end private method End


        /// <summary>
        /// Notifies the specified player's opponent that the player
        /// is typing in their chat box.
        /// </summary>
        /// <param name="player"></param>
        private void NotifyOpponentTyping(Player player)
        {
            player.Opponent.Ss.BeginSend("OPP_TYPING\n", ExceptionCheck, player.Opponent);
        }


        /// <summary>
        /// Sends the specified string to the specified player's opponent.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        private void RelayChatMessage(Player player, string message)
        {
            player.Opponent.Ss.BeginSend("CHAT " + message + "\n", ExceptionCheck, player.Opponent);
        }


        /// <summary>
        /// Changes the length of the game (seconds) to the specified number
        /// and notifies the other player.
        /// </summary>
        /// <param name="s"></param>
        private void ChangeGameLength(Player player, string s)
        {
            int length;
            if (int.TryParse(s, out length))
            {
                gameLength = length;
                player.Opponent.Ss.BeginSend("LENGTH " + length + "\n", ExceptionCheck, player.Opponent);
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
            // stop sending all time updates
            gameTimer.Dispose();
            countDownTimer.Dispose();
            resumeTimer.Dispose();
            watch.Stop(); // elapsed time no longer needed

            // Close socket to offending player
            Player dead = (Player)payload;
            CloseSocket(null, dead.Ss);

            // Notify then close socket to remaining Player (when the remaining
            // player's socket is closed, we'll again execute here and don't want
            // to send terminated to the original disconnecting player)
            if (dead.Opponent.Ss.Connected)
                dead.Opponent.Ss.BeginSend("TERMINATED\n", CloseSocket, dead.Opponent.Ss);

            // print connection lost info
            Console.WriteLine(string.Format("{0, -23} {1, -31} {2}", "CONNECTION LOST", dead.IP, DateTime.Now));
        }


        /// <summary>
        /// Closes the socket if not already done so.
        /// </summary>
        /// <param name="e">NOT USED</param>
        /// <param name="payload">Player Stringsocket to close.</param>
        private void CloseSocket(Exception e, object payload)
        {
            // Close the StringSocket to the Player.
            ((StringSocket)payload).Close();
        }


        /// <summary>
        /// Called when a message has been sent through the StringSocket
        /// to a Player. Exceptions will end this BoggleGame.
        /// </summary>
        /// <param name="e">an Exception, if any</param>
        /// <param name="payload">the Player</param>
        private void ExceptionCheck(Exception e, object payload)
        {
            if (e != null)
                Terminate(e, payload);
        }


        /// <summary>
        /// Updates both players scores appropriately given the specified
        /// word sent from the specified player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="word"></param>
        private void ProcessWord(Player player, string word)
        {
            // Words must be atleast 3 characters long and consist of only letters.
            if (word.Length < 3 || !Regex.IsMatch(word, @"^[A-Z]+$"))
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
                //deleteme.Start();//***************************************************************************************
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
                //deleteme.Stop();//***************************************************************************************
                //Console.WriteLine("ELAPSED: " + deleteme.ElapsedMilliseconds.ToString());//***************************************
            } // end lock
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
        /// Uses the specified HashSet of words to
        /// create a summary string for those words
        /// starting with the size of the Hashset.
        /// </summary>
        /// <param name="set">the set of words</param>
        /// <returns>a summary string</returns>
        private string SetToString(HashSet<string> set)
        {
            StringBuilder words = new StringBuilder(" " + set.Count, 256);
            foreach (string s in set)
            {
                words.Append(" " + s);
            }
            return words.ToString();
        }

        /// <summary>
        /// Returns a string of all the possible legal words 
        /// on this BoggleGame's current board.
        /// </summary>
        private void PossibleWords()
        {
            int count = 0;
            maxScore = 0;
            StringBuilder words = new StringBuilder(2048);
            foreach (string word in BoggleServer.LegalWords)
                if (board.CanBeFormed(word))
                {
                    count++;
                    words.Append(" " + word);
                    maxScore += WordValue(word);
                }
            possibleWords = words.Insert(0, " " + count).ToString();
        }


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

    } // end class BoggleGame
} // end namespace BB
