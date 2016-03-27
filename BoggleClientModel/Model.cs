﻿// Authors: Blake Burton, Cameron Minkel
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
        private TcpClient client; // Allows us to connect to server.
        private StringSocket socket; // Wrapper that takes care of sending strings over socket.
        private const int port = 2000; // The port the server is using.
        public bool playerDisconnected;
        
        // These activate events for the controller to execute.
        public event Action<bool> DisconnectOrErrorEvent; // Event when game is done, resets GUI
        public event Action<string[]> ReceivedBoardEvent; 
        public event Action StartMessageEvent; // Event when START is recieved.
        public event Action<string[]> TimeMessageEvent; // Event when TIME is recieved.
        public event Action<string[]> ScoreMessageEvent; // Event when SCORE is recieved.
        public event Action<List<string[]>> SummaryMessageEvent; // Event when STOP is recieved.
        public event Action SocketExceptionEvent; // Event when socket failed to connect.
        //public event Action ServerClosedEvent; // Event when server closes before game starts.
        public event Action<string> ChatMessageEvent; // Event when chat message received from opponent.
        public event Action<string> ReadyMessageEvent;
        //public event Action OpponentStoppedEvent;
        public event Action PauseEvent;
        public event Action ResumeEvent;
        public event Action<string> CountDownEvent; 


        /// <summary>
        /// Connects the client to the server and let's server know that client is
        /// ready to play. Also begins receiving.
        /// </summary>
        /// <param name="player">The clients name.</param>
        /// <param name="ip">The servers IP Adress.</param>
        public void Connect(string player, string ip)
        {
            try
            {
                client = new TcpClient(ip, port);
                socket = new StringSocket(client.Client, UTF8Encoding.Default);
                //socketOpen = clientOpen = true;//*****************************************************************************************
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
        /// <param name="e">Exeption, if there was one.</param>
        /// <param name="payload">NOT USED.</param>
        private void ReceivedMessage(string s, Exception e, object payload)
        {            
            if (s == null || e != null)
                Terminate(false);
            else if (Regex.IsMatch(s, @"^(TIME\s)")) // Time Update
            {
                TimeUpdate(s);                
            }
            else if (Regex.IsMatch(s, @"^(SCORE\s)")) // Update Score
            {
                ScoreUpdate(s);                
            }
            else if (Regex.IsMatch(s, @"^(CHAT\s)")) // Received chat message
            {
                ReceivedChat(s);
            }
            else if (Regex.IsMatch(s, @"^(COUNTDOWN\s)")) 
            {
                CountDown(s);
            }
            else if (Regex.IsMatch(s, @"^(BOARD\s)"))
            {
                BoardMessage(s);
            }
            else if (Regex.IsMatch(s, @"^(START)")) // Starts Game
            {
                StartMessage();                
            }
            else if (Regex.IsMatch(s, @"^(STOP\s)")) // Game finished
            {
                SummaryMessage(s);               
            }           
            else if (Regex.IsMatch(s, @"^(READY\s)")) // Ready to start
            {
                ReadyMessage(s);
            }
            else if (Regex.IsMatch(s, @"^(PAUSE)")) // Ready to start
            {
                ReceivedPause();
            }
            else if (Regex.IsMatch(s, @"^(RESUME)")) // Ready to start
            {
                ReceivedResume();
            }
            else if (Regex.IsMatch(s, @"^(TERMINATED)")) // Opponent Disconnected
                Terminate(true);          
        }


        /// <summary>
        /// Disconnects and closes the socket and TCPclient.  Activates an event
        /// that allows the GUI to reset things as needed when a disconnection happens.
        /// </summary>
        /// <param name="opponentDisconnected">Allows event to know if player
        ///                                    disconnected or opponent disconnected.</param>
        public void Terminate(bool opponentDisconnected)
        {
            //try
            //{
                socket.Close();
                client.Close();
            //}
            //finally
            //{
                if (!playerDisconnected /*&& (DisconnectOrErrorEvent != null)*/)
                    DisconnectOrErrorEvent(opponentDisconnected);
            //}
        }

        //public void CloseSocket()
        //{
        //    socket.Close();
        //    client.Close();
        //}       


        private void ReadyMessage(string message)
        {
            // send the opponent's name
            ReadyMessageEvent(message.Substring(6));
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        private void BoardMessage(string message)
        {
            char[] spaces = { ' ' }; // Ensures that empty entries are not created.
            string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            ReceivedBoardEvent(tokens);
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        private void CountDown(string message)
        {
            CountDownEvent(message.Substring(10));
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Parses the START message into an array and activates an event for the controller to handle.
        /// VARIENT: The string[] sent to event will contain START in index 0.
        /// </summary>
        /// <param name="message">Message to parse into array.</param>
        private void StartMessage()
        {
            //char[] spaces = { ' ' }; // Ensures that empty entries are not created.
            //string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            StartMessageEvent();
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        /// <summary>
        /// Parses the TIME message into an array and activates an event for the controller to handle.
        /// VARIENT: The string[] sent to event will contain TIME in index 0.
        /// </summary>
        /// <param name="message">Message to parse into array.</param>
        private void TimeUpdate(string message)
        {
            char[] spaces = { ' ' };
            string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);
            TimeMessageEvent(tokens);
            socket.BeginReceive(ReceivedMessage, null);
        }


        /// <summary>
        /// Parses the SCORE message into an array and activates an event for the controller to handle.
        /// VARIENT: The string[] sent to event will contain SCORE in index 0.
        /// </summary>
        /// <param name="message">Message to parse into array.</param>
        private void ScoreUpdate(string message)
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
        /// VARIENT: We do take STOP.
        /// </summary>
        /// <param name="message">Message to parse into array.</param>
        private void SummaryMessage(string message)
        {
            // List to be sent to event.
            List<string[]> results = new List<string[]>();

            message = message.Substring(5); //Takes out Stop

            // Seperate the string into tokens
            char[] spaces = { ' ' };
            string[] tokens = message.Split(spaces, StringSplitOptions.RemoveEmptyEntries);

            int key = 0;
            int tempKey = 0;
            int index = 0;
            string[] temp = new string[key];
            foreach (string s in tokens)
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


        //private void OpponentStopped()
        //{
        //    OpponentStoppedEvent(); 
        //    socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        //}


        /// <summary>
        /// Let's us know that the server recieved something from us.
        /// We simply eat up the message to allow other messages to be recieved.
        /// </summary>
        /// <param name="message">NOT USED.</param>
        //private void IgnoreMessage(string message)
        //{
        //    //IGNORING THE IGNORING!!!!
        //}


        /// <summary>
        /// Sends the words that the players has entered.
        /// </summary>
        /// <param name="word">Word to be sent.</param>
        public void SendWord(string word)
        {
            socket.BeginSend("WORD " + word + "\n", ExceptionCheck, null);
        }


        /// <summary>
        /// Sends the message to be relayed to the opponent
        /// </summary>
        /// <param name="word"></param>
        public void SendChat(string message)
        {
            socket.BeginSend("CHAT " + message + "\n", ExceptionCheck, null);
        }


        private void ReceivedChat(string message)
        {
            ChatMessageEvent(message.Substring(5));
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        public void ClickedPlay()
        {
            socket.BeginSend("PLAY\n", ExceptionCheck, null);
        }


        public void ClickedCancel()
        {
            socket.BeginSend("RETRACT_PLAY\n", ExceptionCheck, null);
        }


        //public void ClickedStop()
        //{
        //    socket.BeginSend("STOP\n", ExceptionCheck, null);
        //}


        public void ClickedPause()
        {
            socket.BeginSend("PAUSE\n", ExceptionCheck, null);
        }


        private void ReceivedPause()
        {
            PauseEvent();
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }


        public void ClickedResume()
        {
            socket.BeginSend("RESUME\n", ExceptionCheck, null);
        }


        private void ReceivedResume()
        {
            ResumeEvent();
            socket.BeginReceive(ReceivedMessage, null); // Receiving Loop
        }




        /// <summary>
        /// Callback to the sockets BeginSend function. Checks to
        /// see if any errors have occured. Terminates if so, nothing if
        /// message was sent succesffuly.
        /// </summary>
        /// <param name="e">Exception thrown, if not null.</param>
        /// <param name="payload">NOT USED.</param>
        private void ExceptionCheck(Exception e, object payload)
        {
            if (e != null)
                Terminate(false);
        }
    }// end Class
} // end Namespace
