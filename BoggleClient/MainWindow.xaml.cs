﻿// Authors: Blake Burton, Cameron Minkel
// Start date: 12/2/14
// Version: 1.0 December 5, 2014 : Finished implenting GUI

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoggleClient
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Model model; // The model to handle socket and computation.


        /// <summary>
        /// Initilizes windows and registers all the events in model.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            model = new Model();
            model.GameEndedEvent += GameEndResetEverything;
            model.StartMessageEvent += GameStartMessage;
            model.TimeMessageEvent += GameTimeMessage;
            model.ScoreMessageEvent += GameScoreMessage;
            model.SummaryMessageEvent += GameSummaryMessage;
            model.SocketExceptionEvent += GameSocketFail;

        }


        /// <summary>
        /// Event to handle the Connect/Disconnect button. Starts as Connect
        /// and will attempt to connect to the server and turn the button into
        /// Disconnect where the button then will attempt to cleanly disconnect
        /// from server and reset GUI as needed.
        /// </summary>
        /// <param name="sender">NOT USED</param>
        /// <param name="e">NOT USED</param>
        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (((string)connectButton.Content) == "Connect")
            { 
                // If player has a an empty name.
                if (playerTextBox.Text == "")
                {
                    infoBox.Text = "Name cannot be empty...\n\n"
                        + "Enter your name and server IP Address then press Connect to play."; 
                    return;
                }

                //Gets GUI elements ready for play.
                playerTextBox.IsEnabled = false;
                serverTextBox.IsEnabled = false;
                infoBox.Text = "Searching for Opponent...";
                model.Connect(playerTextBox.Text, serverTextBox.Text);
                connectButton.Content = "Disconnect";
            }
            else if (((string)connectButton.Content) == "Disconnect")
            {
                // Gets GUI elemtents ready for connection
                infoBox.Visibility = System.Windows.Visibility.Visible;
                infoBox.Text = "You have quit the game...\n\n"
                    + "Enter your name and server IP Address then press Connect to play.";
                model.Terminate(false);
                playerTextBox.IsEnabled = true;
                serverTextBox.IsEnabled = true;            
                connectButton.Content = "Connect";
            }
        }


        /// <summary>
        /// When the player enter a word and presses Enter key.
        /// </summary>
        /// <param name="sender">NOT USED</param>
        /// <param name="e">The Key pressed. Looking only for Enter.</param>
        private void wordEntryBox_Enter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string word = wordEntryBox.Text;
                wordEntryBox.Text = ""; // Clears Box
                model.SendWord(word); // Sends word to server.
            }
        }


        /// <summary>
        /// Invokes the event that responsible for reseting the GUI to it's orginal state.
        /// </summary>
        /// <param name="opponentDisconnect">Lets event know if opponent disconnect from server.</param>
        private void GameEndResetEverything(bool opponentDisconnect)
        {
            Dispatcher.Invoke(new Action(() => { GameEndResetEverythingHelper(opponentDisconnect); }));
        }


        /// <summary>
        /// Resets the GUI to it's orginal state and updats infoBox to let player know of game summery or
        /// disconnections.
        /// </summary>
        /// <param name="opponentDisconnect">Used to determine if opponent disconnected from server.</param>
        private void GameEndResetEverythingHelper(bool opponentDisconnect)
        {
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
            wordEntryBox.IsEnabled = false;
            opponentBox.Text = "";
            timeLeftBox.Text = "";
            pScoreBox.Text = "";
            oScoreBox.Text = "";
            connectButton.Content = "Connect";

            // Will only be hidden if Game did not finish normally.
            if(infoBox.Visibility == System.Windows.Visibility.Hidden)
            {
                // If player disconnected from server.
                if(opponentDisconnect)
                    infoBox.Text = "Your opponent has fled...\n\n"
                        + "Enter your name and server IP Address then press Connect to play.";
                // If connection with server was lost unwillingly.
                else
                    infoBox.Text = "The connection to server was lost.\n\n"
                        + "Enter your name and server IP Address then press Connect to play.";

                infoBox.Visibility = System.Windows.Visibility.Visible;
            }
        }


        /// <summary>
        /// Invokes the event that handles the start of the game.
        /// </summary>
        /// <param name="s">String Tokens containing start game variables from server.</param>
        private void GameStartMessage(string[] s)
        {
            Dispatcher.Invoke(new Action(() => { GameStartMessageHelper(s); }));
        }


        /// <summary>
        /// Invokes the event that handles the start of the game.
        /// Set's up the boggle board letters.
        /// Saves opponents name.
        /// Sets up initial time.
        /// </summary>
        /// <param name="s">String Tokens containing start game variables from server.</param>
        private void GameStartMessageHelper(string[] s)
        {
            // Puts board string onto GUI.
            char[] boggleLetters = s[1].ToCharArray();
            BSpot1.Text = boggleLetters[0] + "";
            BSpot2.Text = boggleLetters[1] + "";
            BSpot3.Text = boggleLetters[2] + "";
            BSpot4.Text = boggleLetters[3] + "";
            BSpot5.Text = boggleLetters[4] + "";
            BSpot6.Text = boggleLetters[5] + "";
            BSpot7.Text = boggleLetters[6] + "";
            BSpot8.Text = boggleLetters[7] + "";
            BSpot9.Text = boggleLetters[8] + "";
            BSpot10.Text = boggleLetters[9] + "";
            BSpot11.Text = boggleLetters[10] + "";
            BSpot12.Text = boggleLetters[11] + "";
            BSpot13.Text = boggleLetters[12] + "";
            BSpot14.Text = boggleLetters[13] + "";
            BSpot15.Text = boggleLetters[14] + "";
            BSpot16.Text = boggleLetters[15] + "";

            // Assigns game time to GUI.
            timeLeftBox.Text = s[2];

            // Assigns opponent name to GUi.
            string opponentName = "";
            for (int i = 3; i < s.Length; i++)
            {
                opponentName += s[i] + " ";
            }
            opponentBox.Text = opponentName;

            // Sets GUI up for when game has begun.
            infoBox.Visibility = System.Windows.Visibility.Hidden;
            pScoreBox.Text = "0";
            oScoreBox.Text = "0";
            wordEntryBox.IsEnabled = true;
            wordEntryBox.Focus();
        }


        /// <summary>
        /// Invokes an event to update time on GUI.
        /// </summary>
        /// <param name="s">Array that contains TIME and actual time.</param>
        private void GameTimeMessage(string[] s)
        {
            Dispatcher.Invoke(new Action(() => { GameTimeMessageHelper(s); }));
        }


        /// <summary>
        /// Event to update time on GUI.
        /// </summary>
        /// <param name="s">Array that contains TIME and actual time.</param>
        private void GameTimeMessageHelper(string[] s)
        {
            timeLeftBox.Text = s[1];
        }


        /// <summary>
        /// Invokes an event to update score on GUI.
        /// </summary>
        /// <param name="s">Array that contains SCORE and actual scores.</param>
        private void GameScoreMessage(string[] s)
        {
            Dispatcher.Invoke(new Action(() => { GameScoreMessageHelper(s); }));
        }


        /// <summary>
        /// Event to update score on GUI.
        /// </summary>
        /// <param name="s">Array that contains SCORE and actual scores.</param>
        private void GameScoreMessageHelper(string[] s)
        {
            pScoreBox.Text = s[1];
            oScoreBox.Text = s[2];
        }


        /// <summary>
        /// Invokes event that ends game and creates summary page.
        /// </summary>
        /// <param name="list">A list of String[] that contains lists of words.</param>
        private void GameSummaryMessage(List<string[]> list)
        {
            Dispatcher.Invoke(() => { GameSummaryMessageHelper(list); });
        }


        /// <summary>
        /// Event that ends game and creates summary page.
        /// </summary>
        /// <param name="list">A list of String[] that contains lists of words.</param>
        private void GameSummaryMessageHelper(List<string[]> list)
        {
            string[] pLegalWords = list[0];
            string[] oLegalWords = list[1];
            string[] sLegalWords = list[2];
            string[] pIllegalWords = list[3];
            string[] oIllegalWords = list[4];

            // Pulls player scores out of GUI.
            int playerScore;
            int opponentScore;
            int.TryParse(pScoreBox.Text, out playerScore);
            int.TryParse(oScoreBox.Text, out opponentScore);

            // Constructs Summary page.
            infoBox.Text = "Time Has Expired!\n";

            if (playerScore > opponentScore)
                infoBox.Text += "WINNER!\n\n";
            else if (playerScore < opponentScore)
                infoBox.Text += "LOSER\n\n";
            else
                infoBox.Text += "BOTH ARE LOSERS!\n\n";

            infoBox.Text += "Game Summary:\n\n"
                + "Player Legal Words\n";
            SummaryPrinter(pLegalWords);

            infoBox.Text += "Player Illegal Words\n";
            SummaryPrinter(pIllegalWords);

            infoBox.Text += "Shared Words\n";
            SummaryPrinter(sLegalWords);

            infoBox.Text += "Opponent Legal Words\n";
            SummaryPrinter(oLegalWords);

            infoBox.Text += "Opponent Illegal Words\n";
            SummaryPrinter(oIllegalWords);

            infoBox.Visibility = System.Windows.Visibility.Visible;        
        }


        /// <summary>
        /// Pulls the strings from the array and string from it.
        /// </summary>
        /// <param name="array">Array with list of words.</param>
        private void SummaryPrinter(string[] array)
        {
            if (array.Length != 0)
            {
                foreach (string s in array)
                {
                    infoBox.Text += s + "\n";
                }
            }
            else
                infoBox.Text += "**NONE**\n";
            infoBox.Text += "\n";
        }


        /// <summary>
        /// Inovkes an event when socket fails to connect.
        /// </summary>
        private void GameSocketFail()
        {
            Dispatcher.Invoke(() => { GameSocketFailHelper(); });
        }


        /// <summary>
        /// Event when socket fails to connect.  Informs players.
        /// </summary>
        private void GameSocketFailHelper()
        {
            infoBox.Text = infoBox.Text = "Unable to connect to server.  Ensure you have"
                + " entered the IP Address correctly.\n\n"
                + "Enter your name and server IP Address then press Connect to play.";
        }
    }
}