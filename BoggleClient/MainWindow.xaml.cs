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
using System.Media;
using System.Threading;

namespace BoggleClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model model; // The model to handle socket and computation.
        private string opponentName;
        private bool resumeClicked;
        private SoundPlayer countSound, incSound, decSound, winSound, lossSound, tieSound, chatSound;
        private BrushConverter converter;
        private Brush initialBlueBrush, yellowBrush; // colors used for the gameRectangle
        private Timer scoreFlashTimer;
        private double scoreFlashOpacity = 1.0;

        /// <summary>
        /// Initilizes windows and registers all the events in model.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            model = new Model();

            // Register the event handlers
            model.ReadyMessageEvent += GameReady;
            model.BoardMessageEvent += SetUpBoard;
            model.CountdownMessageEvent += CountDown;
            model.StartMessageEvent += Start;
            model.TimeMessageEvent += Time;
            model.ScoreMessageEvent += Score;
            model.PauseMessageEvent += Paused;
            model.ResumeMessageEvent += Resumed;
            model.SummaryMessageEvent += GameCompleted;
            model.ChatMessageEvent += ChatMessage;
            model.DisconnectOrErrorEvent += OppDisconnectOrErr;
            model.SocketExceptionEvent += SocketFail;

            // Initialize and load the sounds
            countSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/count.wav");//DISPOSE THESE!!!!!
            incSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/inc.wav");
            decSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/dec.wav");
            winSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/win.wav");
            lossSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/loss.wav");
            tieSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/tie.wav");
            chatSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/chat.wav");
            countSound.Load();
            incSound.Load();
            decSound.Load();
            winSound.Load();
            lossSound.Load();
            tieSound.Load();
            chatSound.Load();
            
            // Initialize Brushes used for the gameRectangle 
            initialBlueBrush = gameRectangle.Fill;
            converter = new BrushConverter();
            yellowBrush = (Brush)converter.ConvertFromString("#FFFFFF94");

            scoreFlashTimer = new Timer(ScoreFlashFadeOut, null, Timeout.Infinite, Timeout.Infinite);
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
            if (connectButton.Content.ToString() == "Connect")
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
                model.playerDisconnected = false;
                infoBox.Text = "You connected to the server.\n\n"
                    + "Waiting for an opponent to connect...";

                // Let model handle connecting to server.
                model.Connect(playerTextBox.Text, serverTextBox.Text);                
            }

            // Disconnect was clicked
            else
            {
                // Reset GUI to its disconnect state.
                connectButton.Content = "Connect";
                opponentBox.Text = "";
                timeLeftBox.Text = "";
                pScoreBox.Text = "";
                oScoreBox.Text = "";
                wordEntryBox.Text = "";
                playerTextBox.IsEnabled = true;
                serverTextBox.IsEnabled = true;
                playButton.IsEnabled = false;
                chatEntryBox.IsEnabled = false;
                wordEntryBox.IsEnabled = false;
                playButton.Content = "Play";
                model.playerDisconnected = true;
                gameRectangle.Fill = initialBlueBrush;
                showBoardButton.Visibility = Visibility.Hidden;
                infoBox.Visibility = Visibility.Visible;
                infoBox.Text = "You disconnected from the server.\n\n"
                    + "Enter your name and server IP Address then click Connect.";

                // Let model handle disconnecting from server.
                model.Terminate(false);
            }
        }


        /// <summary>
        /// Enables client to start game and/or chat.
        /// </summary>
        /// <param name="s"></param>
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
            infoBox.Text = "Your opponent is \"" + s + "\".\n\n"
                + "Chat or click Play to begin!";
        }


        /// <summary>
        /// Handler when Play is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (playButton.Content.ToString() == "Play")
            {
                resumeClicked = false;
                playButton.Content = "Cancel";
                infoBox.Text = infoBox.Text = "Waiting for \"" + opponentName + "\" to click Play...";
                showBoardButton.Visibility = Visibility.Hidden;
                infoBox.Visibility = Visibility.Visible;
                model.ClickedPlay();
            }
            else if (playButton.Content.ToString() == "Cancel")
            {
                if (resumeClicked)
                {
                    playButton.Content = "Resume";
                    infoBox.Text = "Click Resume to continue.";
                }
                else
                {
                    playButton.Content = "Play";
                    infoBox.Text = "Chat or click Play to begin!";
                }
                model.ClickedCancel(resumeClicked);
            }
            else if (playButton.Content.ToString() == "Pause")
            {
                model.ClickedPause();
                playButton.Content = "Resume";
                wordEntryBox.IsEnabled = false;
                infoBox.Text = "You paused the game.\n\nClick Resume to continue.";
                infoBox.Visibility = Visibility.Visible;
            }
            // the Resume button was clicked
            else
            {
                resumeClicked = true;
                playButton.Content = "Cancel";
                infoBox.Text = "Waiting for \"" + opponentName + "\" to click Resume...";
                model.ClickedResume();
            }
        }


        /// <summary>
        /// Sets up the opponent box, time left box, and game board.
        /// </summary>
        /// <param name="tokens"></param>
        private void SetUpBoard(string[] tokens)
        {
            Dispatcher.Invoke(new Action(() => { SetUpBoardHelper(tokens); }));
        }


        private void SetUpBoardHelper(string[] tokens)
        {
            opponentBox.Text = opponentName;
            timeLeftBox.Text = tokens[2];

            // Puts board string onto GUI.
            char[] boggleLetters = tokens[1].ToCharArray();
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
        }


        /// <summary>
        /// Displays a start countdown if "starting" is true.
        /// Otherwise displays a resume countdown.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="starting"></param>
        private void CountDown(string s, bool starting)
        {
            Dispatcher.Invoke(new Action(() => { CountdownHelper(s, starting); }));
        }


        private void CountdownHelper(string s, bool starting)
        {
            double opacity;

            if (s.Equals("3"))
            {
                opacity = 1.0;

                if (starting)
                {
                    startingInLabel.Content = "Starting in...";
                    pScoreBox.Text = "0";
                    oScoreBox.Text = "0";
                    gameRectangle.Fill = yellowBrush;
                }
                else
                    startingInLabel.Content = "Resuming in...";
                
                playButton.IsEnabled = false;                
                countDownLabel.Visibility = Visibility.Visible;
                startingInLabel.Visibility = Visibility.Visible;
                infoBox.Visibility = Visibility.Hidden;
            }
            else if (s.Equals("2"))
                opacity = 0.8;
            else
                opacity = 0.6;
            
            countDownLabel.Content = s;
            countDownLabel.Opacity = opacity;
            startingInLabel.Opacity = opacity;

            //using (var soundPlayer = new SoundPlayer(@"../../../Resources/Resources/Sounds/beep-07.wav"))
            //{
            //    soundPlayer.Play(); // can also use soundPlayer.PlaySync()
            //}

            if (soundOffCheckBox.IsChecked == false) { countSound.Play(); }
        }


        /// <summary>
        /// Displays the game board and enables client for gameplay.
        /// </summary>
        private void Start()
        {
            Dispatcher.Invoke(new Action(() => { StartHelper(); }));
        }


        void StartHelper()
        {
            playButton.Content = "Pause";

            // Sets GUI up for when game has begun.            
            //pScoreBox.Text = "0";//**************************************************************************************
            //oScoreBox.Text = "0";
            wordEntryBox.IsEnabled = true;
            chatEntryBox.IsEnabled = true;
            playButton.IsEnabled = true;//**********************************************************************************
            wordEntryBox.Clear();
            wordEntryBox.Focus();
            countDownLabel.Visibility = Visibility.Hidden;
            startingInLabel.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Updates the current game time.
        /// </summary>
        /// <param name="s"></param>
        private void Time(string s)
        {
            Dispatcher.Invoke(new Action(() => { TimeHelper(s); }));
        }


        private void TimeHelper(string s)
        {
            timeLeftBox.Text = s;
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
        /// Updates both players' scores.
        /// </summary>
        /// <param name="s">Array that contains SCORE and actual scores.</param>
        private void Score(string[] s)
        {
            Dispatcher.Invoke(new Action(() => { ScoreHelper(s); }));
        }


        private void ScoreHelper(string[] s)
        {            
            // Store the old and new scores and the
            // point increase/decrease
            int pOldScore = int.Parse(pScoreBox.Text);
            int pNewScore = int.Parse(s[1]);
            int oOldScore = int.Parse(oScoreBox.Text);
            int oNewScore = int.Parse(s[2]);
            int diff = pNewScore - pOldScore;

            // Update the scores
            pScoreBox.Text = s[1];
            oScoreBox.Text = s[2];

            // Flash the score increase or decrease for a
            // brief moment
            if (diff > 0)
            {
                scoreFlashLabel.Content = "+" + diff;
                scoreFlashLabel.Visibility = Visibility.Visible;
                scoreFlashTimer.Change(200, Timeout.Infinite);
            }
            else if (diff < 0)
            {
                scoreFlashLabel.Content = diff;
                scoreFlashLabel.Visibility = Visibility.Visible;
                scoreFlashTimer.Change(200, Timeout.Infinite);
            }
             

            // Set background color according to who is winning
            if (pNewScore > oNewScore)
                gameRectangle.Fill = new SolidColorBrush(Colors.LightGreen);
            else if (pNewScore < oNewScore)
                gameRectangle.Fill = new SolidColorBrush(Colors.IndianRed);
            else
                gameRectangle.Fill = yellowBrush;

            // Play the appropriate sound
            if (soundOffCheckBox.IsChecked == false)
            {
                if ((pNewScore > pOldScore) || (oNewScore < oOldScore))
                    incSound.Play();
                else
                    decSound.Play();
            }
        }

        // TO FADE OUT THE SCORE, KEEP CALLING TIMER.CHANGE AND SEND HIDESCOREFLASH
        // AN OPACITY WHICH GETS DECREMENTED IN HIDESCOREFLASH AND KEEP LOOPING UNTIL
        // THE OPACITY REACHES ZERO?
        private void ScoreFlashFadeOut(object stateInfo)
        {
            Dispatcher.Invoke(new Action(() => { ScoreFlashFadeOutHelper(); }));
        }


        private void ScoreFlashFadeOutHelper()
        {
            // If score has faded out, reset for next flash and return
            if (scoreFlashOpacity < 0.05)
            {
                scoreFlashLabel.Visibility = Visibility.Hidden;
                scoreFlashOpacity = 1.0;
                scoreFlashLabel.Opacity = 1.0; 
                return;
            }

            scoreFlashLabel.Opacity = scoreFlashOpacity;            
            scoreFlashOpacity -= 0.05;
            scoreFlashTimer.Change(22, Timeout.Infinite);
        }


        /// <summary>
        /// Pauses gameplay.
        /// </summary>
        private void Paused()
        {
            Dispatcher.Invoke(new Action(() => { PausedHelper(); }));
        }


        private void PausedHelper()
        {
            playButton.Content = "Resume";
            wordEntryBox.IsEnabled = false;
            infoBox.Text = infoBox.Text = "\"" + opponentName + "\" paused the game.\n\nClick Resume to continue.";
            infoBox.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// Resumes gameplay.
        /// </summary>
        private void Resumed()
        {
            Dispatcher.Invoke(new Action(() => { ResumedHelper(); }));
        }


        private void ResumedHelper()
        {
            playButton.Content = "Pause";
            wordEntryBox.IsEnabled = true;
            playButton.IsEnabled = true;//**********************************************************************************
            wordEntryBox.Focus();
            countDownLabel.Visibility = Visibility.Hidden;
            startingInLabel.Visibility = Visibility.Hidden;
        }


        /// <summary>
        /// Ends the game and displays the game results
        /// </summary>
        /// <param name="list">A list of String[] that contains lists of words.</param>
        private void GameCompleted(List<string[]> list)
        {
            Dispatcher.Invoke(() => { GameCompletedHelper(list); });
        }


        private void GameCompletedHelper(List<string[]> list)
        {
            playButton.Content = "Play";
            timeLeftBox.Text = "0";

            string[] pLegalWords = list[0];
            string[] oLegalWords = list[1];
            string[] sLegalWords = list[2];
            string[] pIllegalWords = list[3];
            string[] oIllegalWords = list[4];
            string[] possibleWords = list[5];//**************************************************************************

            // Pulls player scores out of GUI.
            int playerScore = int.Parse(pScoreBox.Text);
            int opponentScore = int.Parse(oScoreBox.Text);
            //int.TryParse(pScoreBox.Text, out playerScore);
            //int.TryParse(oScoreBox.Text, out opponentScore);

            // Constructs Summary page.
            infoBox.Text = "Time Has Expired!\n";

            if (playerScore > opponentScore)
            {
                if (soundOffCheckBox.IsChecked == false) { winSound.Play(); }
                infoBox.Text += "WINNER!\n\n";
            }
            else if (playerScore < opponentScore)
            {
                if (soundOffCheckBox.IsChecked == false) { lossSound.Play(); }
                infoBox.Text += "LOSER\n\n";
            }
            else
            {
                if (soundOffCheckBox.IsChecked == false) { tieSound.Play(); }
                infoBox.Text += "BOTH ARE LOSERS!\n\n";
            }

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

            infoBox.Text += "All Possible Words\n";
            SummaryPrinter(possibleWords);

            infoBox.Visibility = Visibility.Visible;
            showBoardButton.Content = "Show Board"; // Reset button
            showBoardButton.Visibility = Visibility.Visible;//*************************************************************

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
        /// Toggles between the game board and message board after games.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showBoardButton_Click(object sender, RoutedEventArgs e)//**************************************************************
        {
            if (showBoardButton.Content.ToString() == "Show Board")
            {
                showBoardButton.Content = "Show Results";
                infoBox.Visibility = Visibility.Hidden;
            }
            else
            {
                showBoardButton.Content = "Show Board";
                infoBox.Visibility = Visibility.Visible;
            }
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
                tr.Text = String.Format("Me ({0})\n", DateTime.Now.ToString("h:mm tt").ToLower());
                tr.ApplyPropertyValue(TextElement.FontSizeProperty, chatEntryBox.FontSize + 2);
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


        /// <summary>
        /// Displays the specified chat message from the opponent.
        /// </summary>
        /// <param name="message"></param>
        private void ChatMessage(string message)
        {
            Dispatcher.Invoke(() => { ChatMessageHelper(message); });
        }


        private void ChatMessageHelper(string message)
        {
            // Format and append an oppononent name & timestamp header to the message box
            TextRange tr = new TextRange(chatDisplayBox.Document.ContentEnd, chatDisplayBox.Document.ContentEnd);
            tr.Text = String.Format("{0} ({1})\n", opponentName, DateTime.Now.ToString("h:mm tt").ToLower());
            tr.ApplyPropertyValue(TextElement.FontSizeProperty, chatEntryBox.FontSize + 2);
            tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.ExtraBold);
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);

            // Append the received message to the message box (not sure if best technique for indenting)
            chatDisplayBox.Document.Blocks.LastBlock.Margin = new Thickness(10, 0, 0, 0); // indent message
            chatDisplayBox.AppendText(message + "\n\n");
            chatDisplayBox.Document.Blocks.LastBlock.Margin = new Thickness(0, 0, 0, 0); // reset for next header
            chatDisplayBox.ScrollToEnd();

            if (soundOffCheckBox.IsChecked == false) { chatSound.Play(); }
        }


        /// <summary>
        /// Handler when chatEntryBox receives focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatEntryBox_GotFocus(object sender, RoutedEventArgs e)
        {
            chatEntryBox.Clear();  // ANY WAY TO AVOID THIS? ONLY NEED TO CLEAR TEXT BOX THE *FIRST* TIME ITS CLICKED **********************
        }


        /// <summary>
        /// Sets the client appropriately when the opponent disconnects
        /// or there is a communication error.
        /// </summary>
        /// <param name="opponentDisconnect"></param>
        private void OppDisconnectOrErr(bool opponentDisconnected)
        {
            Dispatcher.Invoke(new Action(() => { OppDisconnectOrErrHelper(opponentDisconnected); }));
        }


        private void OppDisconnectOrErrHelper(bool opponentDisconnected)
        {
            connectButton.Content = "Connect";
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
            wordEntryBox.IsEnabled = false;
            playButton.IsEnabled = false;
            chatEntryBox.IsEnabled = false;
            playButton.Content = "Play";

            if (opponentDisconnected)
                infoBox.Text = "\"" + opponentName + "\" disconnected from the server and ended your session.\n\n"
                    + "Enter your name and server IP Address then click Connect to play.";
            // If connection with server was lost unwillingly.
            else
                infoBox.Text = "The server closed or there was a communication error.\n\n"
                    + "Enter your name and server IP Address then click Connect to play.";

            // Clear game data and word entry box.
            opponentBox.Text = "";
            timeLeftBox.Text = "";
            pScoreBox.Text = "";
            oScoreBox.Text = "";
            wordEntryBox.Text = "";

            gameRectangle.Fill = initialBlueBrush;
            showBoardButton.Visibility = Visibility.Hidden;
            infoBox.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// Sets client appropriately when connection to server cannot be made.
        /// </summary>
        private void SocketFail()
        {
            Dispatcher.Invoke(() => { SocketFailHelper(); });
        }


        private void SocketFailHelper()
        {
            infoBox.Text = infoBox.Text = "Unable to connect to server. Server may not be "
                + "running or you entered an invalid IP.\n\n"
                + "Enter your name and server IP Address then click Connect.";

            // Allow player to re-enter info.
            connectButton.Content = "Connect";
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
        }
    }
}
