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
        private string opponentName;
        //private bool playerDisconnected;

        /// <summary>
        /// Initilizes windows and registers all the events in model.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            model = new Model();
            model.DisconnectOrErrorEvent += OppDisconnectOrErr;
            model.StartMessageEvent += GameStart;
            model.TimeMessageEvent += GameTime;
            model.ScoreMessageEvent += GameScore;
            model.SummaryMessageEvent += GameCompleted;
            model.SocketExceptionEvent += SocketFail;
            //model.ServerClosedEvent += ServerClosed;
            model.ChatMessageEvent += ChatMessage;
            model.ReadyMessageEvent += GameReady;
            //model.OpponentStoppedEvent += GameStopped;
            model.PauseEvent += GamePaused;
            model.ResumeEvent += GameResumed;
            model.CountDownEvent += GameCountDown;
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

                // Gets GUI elements ready for play.
                connectButton.Content = "Disconnect";
                playerTextBox.IsEnabled = false;
                serverTextBox.IsEnabled = false;
                model.playerDisconnected = false;//**********************************************************************************************
                infoBox.Text = "You connected to the server.\n\n"
                    + "Waiting for an opponent to connect...";

                // Let model handle connecting to server.
                model.Connect(playerTextBox.Text, serverTextBox.Text);
            }
            // Disconnect was clicked
            else
            {
                // Gets GUI elemtents ready for connection.
                connectButton.Content = "Connect";
                playerTextBox.IsEnabled = true;
                serverTextBox.IsEnabled = true;
                playButton.IsEnabled = false;
                chatEntryBox.IsEnabled = false;
                playButton.Content = "Play";
                model.playerDisconnected = true;
                infoBox.Visibility = Visibility.Visible;
                infoBox.Text = "You disconnected from the server.\n\n"
                    + "Enter your name and server IP Address then click Connect.";

                // Let model handle disconnecting from server.
                model.Terminate(false);
                //model.CloseSocket();                
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
            opponentName = s;
            infoBox.Text = "Your opponent is " + s + ".\n\n"
                + "Chat or click Play to begin!";
        }


        private void GameCountDown(string s)
        {
            Dispatcher.Invoke(new Action(() => { GameCountDownHelper(s); }));
        }


        private void GameCountDownHelper(string s)
        {
            // DECIDE WHAT GUI STUFF TO DO
            
            countDownLabel.Content = s;
            countDownLabel.Visibility = Visibility.Visible;
            startingInLabel.Visibility = Visibility.Visible;
            infoBox.Visibility = Visibility.Hidden;
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
            playButton.Content = "Pause";

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
            //timeLeftBox.Text = s[2];

            // Assigns opponent name to GUI.
            string opponentName = "";
            for (int i = 2; i < s.Length; i++)
            {
                opponentName += s[i] + " ";
            }
            opponentBox.Text = opponentName;

            // Sets GUI up for when game has begun.            
            pScoreBox.Text = "0";
            oScoreBox.Text = "0";
            wordEntryBox.IsEnabled = true;
            chatEntryBox.IsEnabled = true;
            wordEntryBox.Focus();
            countDownLabel.Visibility = Visibility.Hidden;
            startingInLabel.Visibility = Visibility.Hidden;
            infoBox.Visibility = Visibility.Hidden;
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


        private void GamePaused()
        {
            Dispatcher.Invoke(new Action(() => { GamePausedHelper(); }));
        }


        private void GamePausedHelper()
        {
            playButton.Content = "Resume"; // WAIT ON BOTH TO CLICK RESUME OR JUST OPPONENT?
            wordEntryBox.IsEnabled = false;
            infoBox.Text = infoBox.Text = opponentName + " paused the game.\n\n";
            infoBox.Visibility = Visibility.Visible;
        }


        private void GameResumed()
        {
            Dispatcher.Invoke(new Action(() => { GameResumedHelper(); }));
        }


        private void GameResumedHelper()
        {
            playButton.Content = "Pause";
            wordEntryBox.IsEnabled = true;
            wordEntryBox.Focus();
            infoBox.Visibility = Visibility.Hidden;
        }


        //private void GameStopped()
        //{
        //    Dispatcher.Invoke(new Action(() => { GameStoppedHelper(); }));
        //}


        //private void GameStoppedHelper()
        //{
        //    playButton.Content = "Play";
        //    infoBox.Text = infoBox.Text = opponentName + " ended the game.\n\n" +
        //            "Chat or click Play to restart.";
        //    infoBox.Visibility = Visibility.Visible;
        //}       


        //private void ServerClosed()
        //{
        //    Dispatcher.Invoke(new Action(() => { ServerClosedHelper(); }));
        //}


        //private void ServerClosedHelper()
        //{
        //    infoBox.Text = "The server closed.\n\n"
        //                + "Enter your name and server IP Address then click Connect.";
        //}        


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
        private void GameCompleted(List<string[]> list)
        {
            Dispatcher.Invoke(() => { GameCompletedHelper(list); });
        }


        /// <summary>
        /// Event that ends game and creates summary page.
        /// </summary>
        /// <param name="list">A list of String[] that contains lists of words.</param>
        private void GameCompletedHelper(List<string[]> list)
        {
            playButton.Content = "Play";
            timeLeftBox.Text = "0";

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

            infoBox.Visibility = Visibility.Visible;

            wordEntryBox.IsEnabled = false;
            playButton.Content = "Play";
        }


        /// <summary>
        /// Pulls the strings from the array and string from it.
        /// </summary>
        /// <param name="array">Array with list of words.</param>
        private void SummaryPrinter(string[] array)
        {
            if (array.Length != 0)
                foreach (string s in array)
                    infoBox.Text += s + "\n";
            else
                infoBox.Text += "**NONE**\n";
            infoBox.Text += "\n";
        }


        /// <summary>
        /// Handler for enter key in the chatbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatBox_Enter(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (chatEntryBox.Text.Trim() != ""))
            {
                // Format and append a "Me:" header to the message box
                TextRange tr = new TextRange(chatDisplayBox.Document.ContentEnd, chatDisplayBox.Document.ContentEnd);
                tr.Text = "Me:\n";
                tr.ApplyPropertyValue(TextElement.FontSizeProperty, chatEntryBox.FontSize + 3);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.ExtraBold);
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);

                // Append the entered message to the message box (not sure if best technique for indenting)
                chatDisplayBox.Document.Blocks.LastBlock.Margin = new Thickness(10, 0, 0, 0); // indent message
                chatDisplayBox.AppendText(chatEntryBox.Text + "\n\n");
                chatDisplayBox.Document.Blocks.LastBlock.Margin = new Thickness(0, 0, 0, 0); // reset for next header
                chatDisplayBox.ScrollToEnd();

                // Send the entered message and clear the entry box
                model.SendChat(chatEntryBox.Text);
                chatEntryBox.Clear();
            }
            else if (e.Key == Key.Enter && (chatEntryBox.Text.Trim() == ""))
                chatEntryBox.CaretIndex = 0; // move caret back to beginning for entered empty strings
        }


        private void ChatMessage(string message)
        {
            Dispatcher.Invoke(() => { ChatMessageHelper(message); });
        }


        private void ChatMessageHelper(string message)
        {
            // Format and append an oppononent name header to the message box
            TextRange tr = new TextRange(chatDisplayBox.Document.ContentEnd, chatDisplayBox.Document.ContentEnd);
            tr.Text = opponentName + ":\n";
            tr.ApplyPropertyValue(TextElement.FontSizeProperty, chatEntryBox.FontSize + 3);
            tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.ExtraBold);
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);

            // Append the received message to the message box (not sure if best technique for indenting)
            chatDisplayBox.Document.Blocks.LastBlock.Margin = new Thickness(10, 0, 0, 0); // indent message
            chatDisplayBox.AppendText(message + "\n\n");
            chatDisplayBox.Document.Blocks.LastBlock.Margin = new Thickness(0, 0, 0, 0); // reset for next header
            chatDisplayBox.ScrollToEnd();
        }


        private void chatEntryBox_GotFocus(object sender, RoutedEventArgs e)
        {
            chatEntryBox.Clear();  // ANY WAY TO AVOID THIS? ONLY NEED TO CLEAR TEXT BOX THE *FIRST* TIME ITS CLICKED
        }


        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (((string)playButton.Content) == "Play")
            {
                playButton.Content = "Cancel";
                infoBox.Text = infoBox.Text = "Waiting for " + opponentName + " to click Play...";
                model.ClickedPlay();
            }
            else if (((string)playButton.Content) == "Cancel")
            {
                playButton.Content = "Play";

                infoBox.Text = "Chat or click Play to begin!";
                model.ClickedCancel();
            }
            else if (((string)playButton.Content) == "Pause")
            {
                model.ClickedPause();
                playButton.Content = "Resume";
                infoBox.Text = "You paused the game.";
                infoBox.Visibility = Visibility.Visible;
            }
            // the Resume button was clicked
            else
            {
                model.ClickedResume();
            }
        }


        /// <summary>
        /// Invokes the event thats responsible for resetting the GUI to it's orginal state.
        /// </summary>
        /// <param name="opponentDisconnect">Lets event know if opponent disconnect from server.</param>
        private void OppDisconnectOrErr(bool opponentDisconnect)
        {
            Dispatcher.Invoke(new Action(() => { OppDisconnectOrErrHelper(opponentDisconnect); }));
        }


        /// <summary>
        /// Resets the GUI to it's original state and updates infoBox to let player know of game summary or
        /// disconnections.
        /// </summary>
        /// <param name="opponentDisconnected">Used to determine if opponent disconnected from server.</param>
        private void OppDisconnectOrErrHelper(bool opponentDisconnected)
        {
            connectButton.Content = "Connect";
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
            wordEntryBox.IsEnabled = false;
            playButton.IsEnabled = false;
            chatEntryBox.IsEnabled = false;
            playButton.Content = "Play";

            // The infoBox will be hidden during gameplay
            //if (infoBox.Visibility == Visibility.Hidden)
            //{
            // If opponent disconnected from server.
            if (opponentDisconnected)
                infoBox.Text = opponentName + " disconnected from the server and ended your session.\n\n"
                    + "Enter your name and server IP Address then click Connect to play.";
            // If connection with server was lost unwillingly.
            else
                infoBox.Text = "The server closed or there was a communication error.\n\n"
                    + "Enter your name and server IP Address then click Connect to play.";

            // DECIDE WHAT I WANT GUI TO DO
            // Clear game data and
            // word entry box.
            //opponentBox.Text = "";
            //timeLeftBox.Text = "";
            //pScoreBox.Text = "";
            //oScoreBox.Text = "";
            wordEntryBox.Text = "";

            infoBox.Visibility = Visibility.Visible;
            //}
            //// the game is over
            //else
            //{
            //    if (opponentDisconnect)
            //        infoBox.Text = opponentBox.Text + " disconnected from the server and ended your session.\n\n"
            //            + "Enter your name and server IP Address then click Connect to play.";
            //}
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
                + "running or you entered an invalid IP.\n\n"
                + "Enter your name and server IP Address then click Connect.";

            // Allow player to re-enter info.
            connectButton.Content = "Connect";
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
            playButton.IsEnabled = false;
        }


       

    }
}
