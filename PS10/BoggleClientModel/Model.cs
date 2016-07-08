// Authors: Blake Burton, Cameron Minkel
// Start date: 12/2/14
// Version 1.0 December 5, 2014:  Finished implementing BoggleClient.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace BoggleClient
{

    /// <summary>
    /// This serves as the model of the MVC of the client side of the Boggle Game.
    /// It handles all the computations and sockets of the client/server relations.
    /// </summary>
    public class Model
    {
        // Class Variables
        private TcpClient client;         // Allows us to connect to server.
        private StringSocket socket;      // Wrapper that takes care of sending strings over socket.
        private const int port = 2000;    // The port the server is using.
        public bool playerDisconnected;
        
        // Events for the controller to handle.        
        public event Action<string> ReadyMessageEvent;
        public event Action<string[]> BoardMessageEvent;
        public event Action<string, bool> CountdownMessageEvent;
        public event Action StartMessageEvent;                   
        public event Action<string> TimeMessageEvent;         
        public event Action<string[]> ScoreMessageEvent;        
        public event Action PauseMessageEvent;                        
        public event Action ResumeMessageEvent;
        public event Action<List<string[]>> SummaryMessageEvent; 
        public event Action<string> ChatMessageEvent;            
        public event Action<bool> DisconnectOrErrorEvent;        
        public event Action SocketExceptionEvent;                 


        /// <summary>
        /// Attempts to connect player to the specified IP.
        /// </summary>
        /// <param name="player">The clients name.</param>
        /// <param name="ip">The servers IP Adress.</param>
        public void Connect(string player, string ip)
        {
            try
            {
                client = new TcpClient(ip, port);
                socket = new StringSocket(client.Client, UTF8Encoding.Default);
                socket.BeginSend("NEW_PLAYER " + player + "\n", ExceptionCheck, null);
                socket.BeginReceive(ReceivedMessage, null);
            }
            catch (SocketException)
            {
                if (SocketExceptionEvent != null)
                    SocketExceptionEvent(); // Lets user know that connection has failed.
            }
        }


        /// <summary>
        /// Handles all socket incoming messages from the server.
        /// </summary>
        /// <param name="s">Message that was received.</param>
        /// <param name="e">Exception, if there was one.</param>
        /// <param name="payload">NOT USED.</param>
        private void ReceivedMessage(string s, Exception e, object payload)
        {            
            if (s == null || e != null)
                Terminate(false);
            else if (Regex.IsMatch(s, @"^(TIME\s)"))            
                ReceivedTime(s);               
            else if (Regex.IsMatch(s, @"^(SCORE\s)"))            
                ReceivedScore(s);            
            else if (Regex.IsMatch(s, @"^(CHAT\s)"))             
                ReceivedChat(s);            
            else if (Regex.IsMatch(s, @"^(COUNTDOWN\s)"))             
                ReceivedCountdown(s, true);            
            else if (Regex.IsMatch(s, @"^(RESUMING\s)"))            
                ReceivedCountdown(s, false);            
            else if (Regex.IsMatch(s, @"^(BOARD\s)"))        
                ReceivedBoard(s);            
            else if (Regex.IsMatch(s, @"^(START)"))            
                ReceivedStart();             
            else if (Regex.IsMatch(s, @"^(STOP\s)"))             
                ReceivedStop(s);                       
            else if (Regex.IsMatch(s, @"^(PAUSE)"))           
                ReceivedPause();            
            else if (Regex.IsMatch(s, @"^(RESUME)"))           
                ReceivedResume();
            else if (Regex.IsMatch(s, @"^(READY\s)"))
                ReceivedReady(s); 
            else if (Regex.IsMatch(s, @"^(TERMINATED)"))
                Terminate(true);          
        }


        /// <summary>
        /// Fires event when an oppenent is ready to play and/or chat.
        /// </summary>
        /// <param name="message"></param>
        private void ReceivedReady(string message)
        {
            ReadyMessageEvent(message.Substring(6)); // send the opponent's name
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Fires event when game board is received.
        /// </summary>
        /// <param name="message"></param>
        private void ReceivedBoard(string message)
        {
            char[] spaces = { ' ' }; // Ensures that empty entries are not created.
            string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            BoardMessageEvent(tokens);
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Fires event when a countdown time is received, whether said
        /// time is for the game starting, or resuming.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="starting"></param>
        private void ReceivedCountdown(string message, bool starting)
        {
            if (starting)
                CountdownMessageEvent(message.Substring(10), starting);
            else
                CountdownMessageEvent(message.Substring(9), starting);
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Fires event when the game has begun.
        /// </summary>
        private void ReceivedStart()
        {
            StartMessageEvent();
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Fires event when a time update is received.
        /// </summary>
        /// <param name="message"></param>
        private void ReceivedTime(string message)
        {
            TimeMessageEvent(message.Substring(5));
            socket.BeginReceive(ReceivedMessage, null);
        }


        /// <summary>
        /// Fires event when a scores update is received.
        /// </summary>
        /// <param name="message"></param>
        private void ReceivedScore(string message)
        {
            char[] spaces = { ' ' };
            string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            ScoreMessageEvent(tokens);
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Parses the STOP message into an List of arrays 
        /// and activates an event for the controller to handle.
        /// Each array contains the list in order
        /// player legal
        /// player illegal
        /// shared words
        /// opponent legal
        /// opponent illegal
        /// all possible words
        /// INVARIENT: We do take STOP.
        /// </summary>
        /// <param name="message">Message to parse into array.</param>
        private void ReceivedStop(string message)
        {
            // List to be sent to event.
            List<string[]> results = new List<string[]>();

            message = message.Substring(5); //Takes out Stop            

            // Seperate the string into tokens
            char[] spaces = { ' ' };
            string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);

            // Add the max possible score to results
            results.Add( new string[]{tokens[0]} ); // STOP 1000

            // Add the different word lists to results
            int key = 0;
            int tempKey = 0;
            int index = 0;
            string[] temp= new string[key];
            foreach (string s in tokens.Skip(1))
            {
                if (int.TryParse(s, out tempKey))
                {
                    key = tempKey;
                    temp = new string[key]; // Uses numbers in message as array size.
                }
                else
                {
                    temp[index] = s; // Adds word to array.
                    index++;
                }

                if (index == key)
                {
                    results.Add(temp); // If array is full, add to List.
                    index = 0;
                }
            }
            
            SummaryMessageEvent(results);
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Sends the word that the player has entered.
        /// </summary>
        /// <param name="word">Word to be sent.</param>
        public void SendWord(string word)
        {
            socket.BeginSend("WORD " + word + "\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Sends the chat message to be relayed to the opponent.
        /// </summary>
        /// <param name="word"></param>
        public void SendChat(string message)
        {
            socket.BeginSend("CHAT " + message + "\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Fires event when a chat message has been received.
        /// </summary>
        /// <param name="message"></param>
        private void ReceivedChat(string message)
        {
            ChatMessageEvent(message.Substring(5));
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Notify server that player wants to start.
        /// </summary>
        public void ClickedPlay()
        {
            socket.BeginSend("PLAY\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Notify server that player cancelled request to start or resume.
        /// </summary>
        /// <param name="resume"></param>
        public void ClickedCancel(bool resume)
        {
            socket.BeginSend("CANCEL " + resume + "\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Notify server that player clicked Pause.
        /// </summary>
        public void ClickedPause()
        {
            socket.BeginSend("PAUSE\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Fires event when opponent clicked Pause.
        /// </summary>
        private void ReceivedPause()
        {
            PauseMessageEvent();
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Notify server that player clicked Resume.
        /// </summary>
        public void ClickedResume()
        {
            socket.BeginSend("RESUME\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Fires event when the game has resumed.
        /// </summary>
        private void ReceivedResume()
        {
            ResumeMessageEvent();
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Callback to the sockets BeginSend function. Checks to
        /// see if any errors have occured. Terminates if so, nothing if
        /// message was sent successfully.
        /// </summary>
        /// <param name="e">Exception thrown, if not null.</param>
        /// <param name="payload">NOT USED.</param>
        private void ExceptionCheck(Exception e, object payload)
        {
            if (e != null)
                Terminate(false);
        }


        /// <summary>
        /// Disconnects and closes the socket and TCPclient.  Activates an event
        /// that allows the GUI to reset things as needed when a disconnection happens.
        /// </summary>
        /// <param name="opponentDisconnected">Allows event to know if player
        ///                                    disconnected or opponent disconnected.</param>
        public void Terminate(bool opponentDisconnected)
        {
            socket.Close();
            client.Close();

            if (!playerDisconnected)
                DisconnectOrErrorEvent(opponentDisconnected);
        } 

    }// end Class
} // end Namespace
