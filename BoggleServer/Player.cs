// Authors: Blake Burton, Cameron Minkel
// Start date: 11/20/14

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomNetworking;

namespace BB
{
    /// <summary>
    /// Represents a player in a Boggle game.
    /// A Player has: a name, opponent, StringSocket
    /// connection with the server, score, and
    /// sets of played legal words, played illegal
    /// words, and shared legal words played by both
    /// the player and opponent.
    /// </summary>
    internal class Player
    {

        /// <summary>
        /// Players name.
        /// </summary>
        public string Name
        { get; private set; }

        /// <summary>
        /// StringSocket connected to server.
        /// </summary>
        public StringSocket Ss
        { get; private set; }

        /// <summary>
        /// Opponent of player.
        /// </summary>
        public Player Opponent
        { get; set; }

        /// <summary>
        /// Current score of player.
        /// </summary>
        public int Score
        { get; set; }
        
        /// <summary>
        /// Legit words that player and opponent have both played.
        /// </summary>
        public HashSet<string> SharedLegalWords
        { get; set; }

        /// <summary>
        /// Words that player has played that are legit.
        /// </summary>
        public HashSet<string> LegalWords
        { get; set; }

        /// <summary>
        /// Words that player has played that are not legit.
        /// </summary>
        public HashSet<string> IllegalWords
        { get; set; }

        /// <summary>
        /// The player's database ID.
        /// </summary>
        public int Id
        { get; set; }

        /// <summary>
        /// Constructer to create player.
        /// </summary>
        /// <param name="s">players name</param>
        /// <param name="ss">Stringsocket that's connected to server.</param>
        public Player(string s, StringSocket ss)
        {
            Name = s;
            Ss = ss;
            Score = 0;
            Opponent = null;
            SharedLegalWords = new HashSet<string>();
            LegalWords = new HashSet<string>();
            IllegalWords = new HashSet<string>();
        }   
    }
}
