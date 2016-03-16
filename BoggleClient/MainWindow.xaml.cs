// Authors: Blake Burton, Cameron Minkel
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

        private Model model; // The model to handle socket and computation.

        /// <summary>
        /// Initilizes windows and registers all the events in model.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            model = new Model();
            model.DisconnectEvent += GameDisconnection;
            model.StartMessageEvent += GameStart;
            model.TimeMessageEvent += GameTime;
            model.ScoreMessageEvent += GameScore;
            model.SummaryMessageEvent += GameSummary;
            model.SocketExceptionEvent += SocketFail;
            model.ServerClosedEvent += ServerClosed;
            model.ChatMessageEvent += ChatMessage;
            model.ReadyMessageEvent += GameReady;
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
            // Clear game data and
            // word entry box.
            opponentBox.Text = "";
            timeLeftBox.Text = "";
            pScoreBox.Text = "";
            oScoreBox.Text = "";
            wordEntryBox.Text = "";

            if (((string)connectButton.Content) == "Connect")
            {
                // If player has a an empty name or server IP.
                if (playerTextBox.Text == "" || serverTextBox.Text == "")
                {
                    infoBox.Text = "Name and/or server IP cannot be empty...\n\n"
                        + "Enter your name and server IP Address then click Connect.";
                    return;
                }

                // Let model handle connecting to server.
                model.Connect(playerTextBox.Text, serverTextBox.Text);

                // Gets GUI elements ready for play.
                connectButton.Content = "Disconnect";
                playerTextBox.IsEnabled = false;
                serverTextBox.IsEnabled = false;
                infoBox.Text = "You have connected to the server.\n\n"
                    + "Waiting for an opponent to connect...";
                
            }            
            //else if (((string)connectButton.Content) == "Disconnect")
            else
            {
                // Gets GUI elemtents ready for connection.
                connectButton.Content = "Connect";
                infoBox.Visibility = System.Windows.Visibility.Visible;
                playerTextBox.IsEnabled = true;
                serverTextBox.IsEnabled = true;
                playButton.IsEnabled = false;
                infoBox.Text = "You have disconnected from the server.\n\n"
                    + "Enter your name and server IP Address then click Connect.";                

                // Let model handle disconnecting from server.
                model.Terminate(false);
            }
        }

        private void GameReady(string s)
        {
            Dispatcher.Invoke(new Action(() => { GameReadyHelper(s); }));
        }


        private void GameReadyHelper(string s)
        {
            playerTextBox.IsEnabled = false;
            serverTextBox.IsEnabled = false;
            chatEntryBox.IsEnabled = true;
            playButton.IsEnabled = true;
            opponentBox.Text = s;
            infoBox.Text = s + " is connected to the server.\n\n"
                + "Chat or click Play to begin!";            
        }


        /// <summary>
        /// Invokes the event that handles the start of the game.
        /// </summary>
        /// <param name="s">String Tokens containing start game variables from server.</param>
        private void GameStart(string[] s)
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
            // Allow user to quit game.
            connectButton.Content = "Disconnect";

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

            // Assigns opponent name to GUI.
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
            chatEntryBox.IsEnabled = true;
            wordEntryBox.Focus();
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
        /// Invokes the event thats responsible for resetting the GUI to it's orginal state.
        /// </summary>
        /// <param name="opponentDisconnect">Lets event know if opponent disconnect from server.</param>
        private void GameDisconnection(bool opponentDisconnect)
        {
            Dispatcher.Invoke(new Action(() => { GameDisconnectionHelper(opponentDisconnect); }));
        }


        /// <summary>
        /// Resets the GUI to it's original state and updates infoBox to let player know of game summary or
        /// disconnections.
        /// </summary>
        /// <param name="opponentDisconnect">Used to determine if opponent disconnected from server.</param>
        private void GameDisconnectionHelper(bool opponentDisconnect)
        {
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
            wordEntryBox.IsEnabled = false;
            connectButton.Content = "Connect";
            playButton.Content = "Play";

            // The infoBox will be hidden during gameplay
            if (infoBox.Visibility == System.Windows.Visibility.Hidden)
            {
                // If opponent disconnected from server.
                if (opponentDisconnect)
                    infoBox.Text = opponentBox.Text + " disconnected from the server and ended your session.\n\n"
                        + "Enter your name and server IP Address then click Connect to play.";
                // If connection with server was lost unwillingly.
                else
                    infoBox.Text = "The connection to the server was lost.\n\n"
                        + "Enter your name and server IP Address then click Connect to play.";
                
                // Clear game data and
                // word entry box.
                //opponentBox.Text = "";
                //timeLeftBox.Text = "";
                //pScoreBox.Text = "";
                //oScoreBox.Text = "";
                //wordEntryBox.Text = "";               

                infoBox.Visibility = System.Windows.Visibility.Visible;
            }
            // the game is over
            else
            {
                if (opponentDisconnect)
                    infoBox.Text = opponentBox.Text + " disconnected from the server and ended your session.\n\n"
                        + "Enter your name and server IP Address then click Connect to play.";
            }
        }


        private void ServerClosed()
        {
            Dispatcher.Invoke(new Action(() => { ServerClosedHelper(); }));
        }


        private void ServerClosedHelper()
        {
            infoBox.Text = "The server closed.\n\n"
                        + "Enter your name and server IP Address then click Connect.";
        }        


        /// <summary>
        /// Invokes an event to update time on GUI.
        /// </summary>
        /// <param name="s">Array that contains TIME and actual time.</param>
        private void GameTime(string[] s)
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
        private void GameScore(string[] s)
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
        private void GameSummary(List<string[]> list)
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

            infoBox.Text += "*** Game Summary ***\n\n"
                + "Your Legal Words\n";
            SummaryPrinter(pLegalWords);

            infoBox.Text += "Your Illegal Words\n";
            SummaryPrinter(pIllegalWords);

            infoBox.Text += "Shared Words\n";
            SummaryPrinter(sLegalWords);

            infoBox.Text += "Opponent Legal Words\n";
            SummaryPrinter(oLegalWords);

            infoBox.Text += "Opponent Illegal Words\n";
            SummaryPrinter(oIllegalWords);

            infoBox.Visibility = System.Windows.Visibility.Visible;

            playButton.Content = "Play";
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
        private void SocketFail()
        {
            Dispatcher.Invoke(() => { GameSocketFailHelper(); });
        }


        /// <summary>
        /// Event when socket fails to connect.  Informs players.
        /// </summary>
        private void GameSocketFailHelper()
        {
            infoBox.Text = infoBox.Text = "Unable to connect to server. Server may not be "
                + "running or you have entered an invalid IP.\n\n"
                + "Enter your name and server IP Address then click Connect.";

            // Allow player to re-enter info.
            connectButton.Content = "Connect";
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
            playButton.IsEnabled = false;
        }


        /// <summary>
        /// Handler for enter key in the chatbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatBox_Enter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                 //model.SendMessage(chatBox.selection);
            }
        }


        private void ChatMessage(string message)
        {
            Dispatcher.Invoke(() => { ChatMessageHelper(message); });
        }

        private void ChatMessageHelper(string message)
        {
            //chatBox.AppendText(message);
        }

        private void chatEntryBox_GotFocus(object sender, RoutedEventArgs e)
        {
            chatEntryBox.Clear();
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (((string)playButton.Content) == "Play")
            {
                playButton.Content = "Quit";
                infoBox.Text = infoBox.Text = "Waiting for opponent to click Play...";
                model.ClickedPlay();
            }
            else
            {

            }
        }

    }
}
