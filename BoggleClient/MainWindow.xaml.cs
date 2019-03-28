// Authors: Blake Burton, Cameron Minkel
// Start date: 12/2/14
// Version: 1.0 December 5, 2014 : Finished implenting GUI

using System;
using System.Collections.Generic;
//using System.Linq;                  // ********************************************** DELETE ME?
//using System.Text;                  // ********************************************** DELETE ME?
//using System.Threading.Tasks;       // ********************************************** DELETE ME?
using System.Threading;
using System.Windows;
//using System.Windows.Controls;      // ********************************************** DELETE ME?
//using System.Windows.Data;          // ********************************************** DELETE ME?
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
//using System.Windows.Media.Imaging; // ********************************************** DELETE ME?
//using System.Windows.Navigation;    // ********************************************** DELETE ME?
//using System.Windows.Shapes;        // ********************************************** DELETE ME?
using System.Media;
using System.IO;

namespace BoggleClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Model model; // The model to handle socket and computation.
        private string opponentName;
        private bool chatInitialSelection = true;
        private int chatKeyCount = 0;
        private SoundPlayer countSound, /*countSound2,*/ incSound, decSound, winSound, lossSound, tieSound, tieSound2, chatSound;
        private Brush defaultBrush, yellowBrush; // colors used for the gameRectangle  
        private Timer pointFlashTimer, opponentTypingTimer;
        private TextRange opponentTypingTR; // "opponent is typing" notification in the chat box
        private double pointFlashOpacity = 1.0;
        private int wins, ties, losses;

        /// <summary>
        /// Initilizes windows and registers all the events in model.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            model = new Model();
            BrushConverter converter;
            string rulesFileName = "../../../Resources/Resources/Rules.rtf";
            //string rulesFileName = "Resources/Rules.rtf"; // USE THIS WHEN BUILDING THE CLIENT INSTALLER
            TextRange textRange;
            FileStream fileStream;

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
            model.OpponentTypingEvent += OpponentTyping;
            model.ChatMessageEvent += ChatMessage;
            model.GameLengthEvent += GameLength;
            model.DisconnectOrErrorEvent += OppDisconnectOrErr;
            model.SocketExceptionEvent += SocketFail;

            // Initialize and load the sounds
            countSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/countdown.wav");
            //countSound2 = new SoundPlayer(@"../../../Resources/Resources/Sounds/countdown2.wav"); // FINAL SECONDS BEEP
            incSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/inc.wav");
            decSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/dec.wav");
            winSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/win.wav");
            lossSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/loss.wav");
            tieSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/tie.wav");
            tieSound2 = new SoundPlayer(@"../../../Resources/Resources/Sounds/tie2.wav");
            chatSound = new SoundPlayer(@"../../../Resources/Resources/Sounds/chat.wav");

            // USE THESE WHEN BUILDING THE CLIENT INSTALLER
            //countSound = new SoundPlayer(@"Resources/Sounds/countdown.wav");
            ////countSound2 = new SoundPlayer(@"countdown2.wav"); // FINAL SECONDS BEEP
            //incSound = new SoundPlayer(@"Resources/Sounds/inc.wav");
            //decSound = new SoundPlayer(@"Resources/Sounds/dec.wav");
            //winSound = new SoundPlayer(@"Resources/Sounds/win.wav");
            //lossSound = new SoundPlayer(@"Resources/Sounds/loss.wav");
            //tieSound = new SoundPlayer(@"Resources/Sounds/tie.wav");
            //tieSound2 = new SoundPlayer(@"Resources/Sounds/tie2.wav");
            //chatSound = new SoundPlayer(@"Resources/Sounds/chat.wav");

            try
            {
                countSound.Load();
                incSound.Load();
                decSound.Load();
                winSound.Load();
                lossSound.Load();
                tieSound.Load();
                tieSound2.Load();
                chatSound.Load();
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show(e.Message + "\n\nSounds are disabled.", "Boggle Legends Deluxe Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                soundOffCheckBox.IsChecked = true;
                soundOffCheckBox.IsEnabled = false;
            }

            // Initialize Brushes (colors) used for the gameRectangle 
            defaultBrush = gameRectangle.Fill;
            converter = new BrushConverter();
            yellowBrush = (Brush)converter.ConvertFromString("#FFFFFF94");

            // Initialize Timers
            pointFlashTimer = new Timer(PointFlashFadeOut, null, Timeout.Infinite, Timeout.Infinite);
            opponentTypingTimer = new Timer(RemoveOppTypNotification, null, Timeout.Infinite, Timeout.Infinite);

            // Load "Rules.rtf" into the rules box
            textRange = new TextRange(rulesBox.Document.ContentStart, rulesBox.Document.ContentEnd);
            try
            {
                using (fileStream = new FileStream(rulesFileName, FileMode.Open, FileAccess.Read))
                {
                    textRange.Load(fileStream, DataFormats.Rtf);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\n\n\"Show Rules\" button is disabled.", "Boggle Legends Deluxe Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                showRulesButton.IsEnabled = false;
            }
            serverTextBox.Focus(); // put cursor in server box
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
                        + "Enter your name and server IP address then click Connect.";
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
                lengthTextBox.IsEnabled = false;
                lengthTextBox.Clear();
                setButton.IsEnabled = false;
                gameRectangle.Fill = defaultBrush;
                showBoardButton.Visibility = Visibility.Hidden;
                infoBox.Visibility = Visibility.Visible;
                infoBox.Text = "You disconnected from the server.\n\n"
                    + "Enter your name and server IP address then click Connect.";

                // Let model handle disconnecting from server.
                model.Terminate(false);
            }
        }


        /// <summary>
        /// Enables client to start game and/or chat.
        /// </summary>
        /// <param name="s"></param>
        private void GameReady(string[] tokens)
        {
            Dispatcher.Invoke(new Action(() => { GameReadyHelper(tokens); }));
        }


        private void GameReadyHelper(string[] tokens)
        {
            playerTextBox.IsEnabled = false;
            serverTextBox.IsEnabled = false;
            chatEntryBox.IsEnabled = true;
            playButton.IsEnabled = true;
            lengthTextBox.IsEnabled = true;
            setButton.IsEnabled = true;
            wins = losses = ties = 0;
            opponentName = tokens[1];
            lengthTextBox.Text = tokens[2];
            infoBox.Text = "Your opponent is \"" + tokens[1] + "\".\n\n"
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
                playButton.Content = "Cancel Play";
                infoBox.Text = infoBox.Text = "Waiting for \"" + opponentName + "\" to click Play...";
                showBoardButton.Visibility = Visibility.Hidden;
                infoBox.Visibility = Visibility.Visible;
                model.ClickedPlay();
            }
            else if (playButton.Content.ToString() == "Cancel Play")
            {
                playButton.Content = "Play";
                infoBox.Text = "Chat or click Play to begin!";
                model.ClickedCancel(false);
            }
            else if (playButton.Content.ToString() == "Pause")
            {
                model.ClickedPause();
                playButton.Content = "Resume";
                wordEntryBox.IsEnabled = false;
                infoBox.Text = "You paused the game.\n\nClick Resume to continue.";
                infoBox.Visibility = Visibility.Visible;
            }
            else if (playButton.Content.ToString() == "Resume")
            {
                playButton.Content = "Cancel Resume";
                infoBox.Text = "Waiting for \"" + opponentName + "\" to click Resume...";
                model.ClickedResume();
            }
            // the Cancel Resume button was clicked
            else
            {
                playButton.Content = "Resume";
                infoBox.Text = "Click Resume to continue.";
                model.ClickedCancel(true);
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
            lengthTextBox.IsEnabled = false;
            setButton.IsEnabled = false;

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

            if (soundOffCheckBox.IsChecked == false) countSound.Play();
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
            wordEntryBox.IsEnabled = true;
            chatEntryBox.IsEnabled = true;
            playButton.IsEnabled = true;
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

            // IF I WANT A TIME RUNNING OUT SOUND...
            //if ((int.Parse(s) <= 5) && (soundOffCheckBox.IsChecked == false)) 
            //    countSound2.Play();
        }


        /// <summary>
        /// When the player enter a word and presses Enter key.
        /// </summary>
        /// <param name="sender">NOT USED</param>
        /// <param name="e">The Key pressed. Looking only for Enter.</param>
        private void wordEntryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string word = wordEntryBox.Text;
                wordEntryBox.Clear(); // Clears Box
                model.SendWord(word); // Sends word to server
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

            // Briefly flash and fade out the point increase/decrease
            if (pntFlashesOffCheckBox.IsChecked == false)
            {
                if (diff != 0)
                {
                    if (diff > 0)
                    {
                        pointFlashLabel.Foreground = Brushes.Green;
                        pointFlashLabel.Content = "+" + diff;
                    }
                    else
                    {
                        pointFlashLabel.Foreground = Brushes.Red;
                        pointFlashLabel.Content = diff;
                    }

                    pointFlashLabel.Visibility = Visibility.Visible;
                    pointFlashTimer.Change(200, Timeout.Infinite);
                }
            }

            // Set background color according to who is winning
            if (pNewScore > oNewScore)
                gameRectangle.Fill = new SolidColorBrush(Colors.LightGreen);
            else if (pNewScore < oNewScore)
                gameRectangle.Fill = new SolidColorBrush(Colors.IndianRed);
            else
                gameRectangle.Fill = yellowBrush;

            // Update the scores
            pScoreBox.Text = s[1];
            oScoreBox.Text = s[2];

            // Play the appropriate sound
            if (soundOffCheckBox.IsChecked == false)
            {
                if ((pNewScore > pOldScore) || (oNewScore < oOldScore))
                    incSound.Play();
                else
                    decSound.Play();
            }
        }


        /// <summary>
        /// Fades out the point flash label.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void PointFlashFadeOut(object stateInfo)
        {
            Dispatcher.Invoke(new Action(() => { PointFlashFadeOutHelper(); }));
        }


        private void PointFlashFadeOutHelper()
        {
            // If score has faded out, reset for next flash and return.
            // Note: fade out duration = 22 * (1.0/0.05) = 440 ms
            if (pointFlashOpacity < 0.05)
            {
                pointFlashLabel.Visibility = Visibility.Hidden;
                pointFlashOpacity = 1.0;
                pointFlashLabel.Opacity = 1.0;
                return;
            }

            pointFlashLabel.Opacity = pointFlashOpacity;
            pointFlashOpacity -= 0.05;
            pointFlashTimer.Change(22, Timeout.Infinite);
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
            playButton.IsEnabled = true;
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

            string maxScore = list[0][0];
            string[] pLegalWords = list[1];
            string[] oLegalWords = list[2];
            string[] sLegalWords = list[3];
            string[] pIllegalWords = list[4];
            string[] oIllegalWords = list[5];
            string[] possibleWords = list[6];

            // Pulls player scores out of GUI.
            int playerScore = int.Parse(pScoreBox.Text);
            int opponentScore = int.Parse(oScoreBox.Text);

            // Constructs Summary page.
            if (playerScore > opponentScore)
            {
                if (soundOffCheckBox.IsChecked == false) winSound.Play();
                infoBox.Text = "*** WINNER! ***\n\n";
                wins++;
            }
            else if (playerScore < opponentScore)
            {
                if (soundOffCheckBox.IsChecked == false) lossSound.Play();
                infoBox.Text = "*** LOSER ***\n\n";
                losses++;
            }
            else
            {
                if (soundOffCheckBox.IsChecked == false)
                {
                    // The "Price is Wrong" sound only plays when
                    // the tie score isn't zero and divisible by 3.
                    if ((playerScore != 0) && (playerScore % 3 == 0))
                        tieSound2.Play();
                    else
                        tieSound.Play();
                }
                infoBox.Text = "*** TIE ***\n\n";
                ties++;
            }

            infoBox.Text += "Wins: " + wins + "\n";
            infoBox.Text += "Losses: " + losses + "\n";
            infoBox.Text += "Ties: " + ties + "\n\n";

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

            infoBox.Text += "All Possible Words\n(" + possibleWords.Length +
                " Words, " + maxScore + " Total Points)\n";
            SummaryPrinter(possibleWords);

            infoBox.Visibility = Visibility.Visible;
            showBoardButton.Content = "Show Board"; // Reset button
            showBoardButton.Visibility = Visibility.Visible;

            wordEntryBox.IsEnabled = false;
            playButton.Content = "Play";
            lengthTextBox.IsEnabled = true;
            setButton.IsEnabled = true;
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
                infoBox.Text += "*NONE*\n";
            infoBox.Text += "\n";
        }


        /// <summary>
        /// Handler when game length box is clicked. Text turns bold and green to indicate editing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lengthTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            lengthTextBox.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0CCD15"));
            lengthTextBox.FontWeight = FontWeights.Bold;
        }


        /// <summary>
        /// Allows only digits and spaces to be entered in lengthTextBox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LengthValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !model.IsDigit(e.Text);
        }


        /// <summary>
        /// Disallows spaces to be entered in lengthTextBox b/c above function is not sufficient
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lengthTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }


        /// <summary>
        /// Handler when Set is clicked. Send the new game length to the server.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void setButton_Click(object sender, RoutedEventArgs e)
        {
            model.SendGameLength(lengthTextBox.Text);
            ResetGameLengthTextStyle();
        }


        private void lengthTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                model.SendGameLength(lengthTextBox.Text);
                ResetGameLengthTextStyle();

                // Stackoverflow code: moves focus away from game length box to a parent.
                // I think this better indicates to the user that the length was actually set.
                FrameworkElement parent = (FrameworkElement)lengthTextBox.Parent;
                while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable)
                {
                    parent = (FrameworkElement)parent.Parent;
                }
                DependencyObject scope = FocusManager.GetFocusScope(lengthTextBox);
                FocusManager.SetFocusedElement(scope, parent as IInputElement);
            }
        }


        private void ResetGameLengthTextStyle()
        {
            lengthTextBox.Foreground = Brushes.Black;
            lengthTextBox.FontWeight = FontWeights.Normal;
        }


        /// <summary>
        /// Toggles between the game board and message board after games.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showBoardButton_Click(object sender, RoutedEventArgs e)
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
        /// Toggles between showing or hiding the rules document.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showRulesButton_Click(object sender, RoutedEventArgs e)
        {
            if (showRulesButton.Content.ToString() == "Show Rules")
            {
                showRulesButton.Content = "Hide Rules";
                rulesBox.Visibility = Visibility.Visible;
            }
            else
            {
                showRulesButton.Content = "Show Rules";
                rulesBox.Visibility = Visibility.Hidden;
            }
        }


        /// <summary>
        /// Handler when chatEntryBox receives focus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatEntryBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (chatInitialSelection)
            {
                chatEntryBox.Clear();
                chatInitialSelection = false;
            }
        }


        /// <summary>
        /// Handler for keys typed in the chatbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Nonempty string entered
            if (e.Key == Key.Enter && (chatEntryBox.Text.Trim() != ""))
            {
                // Get rid of opponent typing notification if it is present
                if (opponentTypingTR != null && !opponentTypingTR.IsEmpty)
                {
                    opponentTypingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    opponentTypingTR.Text = "";
                }
                chatKeyCount = 0; // reset key count for opponent typing notification

                // Post a "Me:" timestamp header to the message box
                string meHeaderText = String.Format("Me ({0})\n", DateTime.Now.ToString("h:mm tt").ToLower());
                TextRange meHeader = PostChatBoxHeader(meHeaderText, 14.0, FontStyles.Normal, FontWeights.ExtraBold, Brushes.Green);

                // Indent and add the entered message underneath the header
                TextRange myMessage = new TextRange(meHeader.End, meHeader.End);
                myMessage.Start.Paragraph.Margin = new Thickness(10, 0, 0, 0);
                myMessage.Text = chatEntryBox.Text + "\n\n";
                myMessage.ClearAllProperties();
                myMessage.End.Paragraph.Margin = new Thickness(0, 0, 0, 0);

                // Send the entered message and clear the entry box
                model.SendChat(chatEntryBox.Text);
                chatEntryBox.Clear();
            }

            // Empty string entered (move caret back to beginning)
            else if (e.Key == Key.Enter && (chatEntryBox.Text.Trim() == ""))
                chatEntryBox.CaretIndex = 0;

            // A regular key is pressed. Send opponent typing notification if necessary.
            else
            {
                if (++chatKeyCount == 1) // 1st key down
                {
                    model.SendTypingNotification();
                }
                else if (chatKeyCount == 15) // 15th key down
                {
                    chatKeyCount = -14; // reset
                    model.SendTypingNotification();
                }
            }
        }


        /// <summary>
        /// Notifies this player that their opponent is typing in their chat box.
        /// </summary>
        private void OpponentTyping()
        {
            Dispatcher.Invoke(() => { OpponentTypingHelper(); });
        }


        private void OpponentTypingHelper()
        {
            // Only post the notification if one isn't already posted
            if ((opponentTypingTR == null) || opponentTypingTR.IsEmpty)
            {
                opponentTypingTR = PostChatBoxHeader("opponent is typing ...\n\n", 14.0, FontStyles.Italic, FontWeights.ExtraBold, Brushes.Blue);
                opponentTypingTimer.Change(2000, Timeout.Infinite); // remove notification after 2 seconds
            }
        }


        /// <summary>
        /// Removes the opponent is typing notification from the top of the chat box.
        /// </summary>
        /// <param name="stateInfo"></param>
        private void RemoveOppTypNotification(object stateInfo)
        {
            Dispatcher.Invoke(new Action(() => { RemoveOppTypNotificationHelper(); }));
        }


        private void RemoveOppTypNotificationHelper()
        {
            chatKeyCount = 0;
            opponentTypingTR.Text = "";
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
            // Remove opponent typing notification if one is posted
            if (opponentTypingTR != null && !opponentTypingTR.IsEmpty)
            {
                opponentTypingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                chatKeyCount = 0;
                opponentTypingTR.Text = "";
            }

            // Post an oppononent name timestamp header to the message box
            string oppHeaderText = String.Format("{0} ({1})\n", opponentName, DateTime.Now.ToString("h:mm tt").ToLower());
            TextRange oppHeader = PostChatBoxHeader(oppHeaderText, 14.0, FontStyles.Normal, FontWeights.ExtraBold, Brushes.Red);

            // Indent and add the received message underneath the header
            TextRange oppMessage = new TextRange(oppHeader.End, oppHeader.End);
            oppMessage.Start.Paragraph.Margin = new Thickness(10, 0, 0, 0);
            oppMessage.Text = message + "\n\n";
            oppMessage.ClearAllProperties();
            oppMessage.End.Paragraph.Margin = new Thickness(0, 0, 0, 0);

            if (soundOffCheckBox.IsChecked == false) chatSound.Play();
        }


        /// <summary>
        /// Posts and returns a TextRange header to the top of the chat box with the specified properties.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="size"></param>
        /// <param name="style"></param>
        /// <param name="weight"></param>
        /// <param name="color"></param>
        /// <returns>the created header</returns>
        private TextRange PostChatBoxHeader(string text, double size, FontStyle style, FontWeight weight, Brush color)
        {
            TextRange header = new TextRange(chatDisplayBox.Document.ContentStart, chatDisplayBox.Document.ContentStart);
            header.Text = text;
            header.ApplyPropertyValue(TextElement.FontSizeProperty, size);
            header.ApplyPropertyValue(TextElement.FontStyleProperty, style);
            header.ApplyPropertyValue(TextElement.FontWeightProperty, weight);
            header.ApplyPropertyValue(TextElement.ForegroundProperty, color);

            return header;
        }        


        /// <summary>
        /// Displays the newly set game time length (seconds)
        /// </summary>
        /// <param name="length"></param>
        private void GameLength(string length)
        {
            Dispatcher.Invoke(new Action(() => { GameLengthHelper(length); }));
        }


        private void GameLengthHelper(string length)
        {
            lengthTextBox.Text = length;
            ResetGameLengthTextStyle();
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
            lengthTextBox.IsEnabled = false;
            lengthTextBox.Clear();
            setButton.IsEnabled = false;
            playButton.Content = "Play";

            if (opponentDisconnected)
                infoBox.Text = "\"" + opponentName + "\" disconnected from the server and ended your session.\n\n"
                    + "Enter your name and server IP Address then click Connect to play.";
            // Connection with server was lost unwillingly.
            else
                infoBox.Text = "The server closed or there was a communication error.\n\n"
                    + "Enter your name and server IP Address then click Connect to play.";

            // Clear game data and word entry box.
            opponentBox.Text = "";
            timeLeftBox.Text = "";
            pScoreBox.Text = "";
            oScoreBox.Text = "";
            wordEntryBox.Text = "";

            gameRectangle.Fill = defaultBrush;
            showBoardButton.Visibility = Visibility.Hidden;
            infoBox.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// Sets client appropriately when connection to server cannot be made.
        /// </summary>
        private void SocketFail(string message)
        {
            Dispatcher.Invoke(() => { SocketFailHelper(message); });
        }


        private void SocketFailHelper(string message)
        {
            infoBox.Text = infoBox.Text = "Unable to connect to server. Perhaps the server is not "
                + "running or you entered an invalid IP:\n\n\""
                + message + "\""
                + "\n\nEnter your name and server IP Address then click Connect.";

            // Allow player to re-enter info.
            connectButton.Content = "Connect";
            playerTextBox.IsEnabled = true;
            serverTextBox.IsEnabled = true;
        }


        /// <summary>
        /// Perhaps not necessary, but disposes all sounds and timers when application is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            countSound.Dispose();
            incSound.Dispose();
            decSound.Dispose();
            winSound.Dispose();
            lossSound.Dispose();
            tieSound.Dispose();
            tieSound2.Dispose();
            chatSound.Dispose();
            pointFlashTimer.Dispose();
            opponentTypingTimer.Dispose();
            model.Terminate(false);
        }        
    }
}
