using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BB
{
    /// <summary>
    /// Represents a Boggle board.
    /// </summary>
    public class BoggleBoard
    {
        // The 4x4 Boggle board
        private char[,] board;

        // The 16 cubes that make up a standard Boggle Board
        private string[] cubes = 
            {
                "LRYTTE",
                "ANAEEG",
                "AFPKFS",
                "YLDEVR",
                "VTHRWE",
                "IDSYTT",
                "XLDERI",
                "ZNRNHL",
                "EGHWNE",
                "OATTOW",
                "HCPOAS",
                "OBBAOJ",
                "SEOTIS",
                "MTOICU",
                "ENSIEU",
                "NMIQHU"
            };

        /// <summary>
        /// Creates a randomly-generated BoggleBoard 
        /// </summary>
        public BoggleBoard()
        {
            // Shuffle the cubes
            Random r = new Random();
            for (int i = cubes.Length - 1; i >= 0; i--)
            {
                int j = r.Next(i + 1);
                string temp = cubes[i];
                cubes[i] = cubes[j];
                cubes[j] = temp;
            }

            // Make a string by choosing one character at random
            // from each cube.
            string letters = "";
            for (int i = 0; i < cubes.Length; i++)
            {
                letters += cubes[i][r.Next(6)];
            }

            // Make the board
            MakeBoard(letters);
        }

        /// <summary>
        /// Creates a BoggleBoard from the provided 16-letter string.  The
        /// method is case-insensitive.  If there aren't exactly 16 letters
        /// in the string, throws an ArgumentException.  The string consists
        /// of the first row, then the second row, then the third, then the fourth.
        /// </summary>
        public BoggleBoard(string letters)
        {
            // Use upper case
            letters = letters.ToUpper();

            // Make sure letters are legal
            if (letters.Length != 16)
            {
                throw new ArgumentException();
            }
            foreach (char c in letters)
            {
                if (!Char.IsLetter(c))
                {
                    throw new ArgumentException();
                }
            }

            // Make the board
            MakeBoard(letters);
        }

        /// <summary>
        /// Makes a board from the 16-letter string
        /// </summary>
        private void MakeBoard(string letters)
        {
            board = new char[4, 4];
            int index = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    board[i, j] = letters[index++];
                }
            }
        }


        /// <summary>
        /// Returns the 16 letters that make up this board.  It is formed
        /// by appending the first row to the second row to the third row
        /// to the fourth row.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string letters = "";
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    letters += board[i, j];
                }
            }
            return letters;
        }


        /// <summary>
        /// Reports whether the provided word can be formed by tracking through
        /// this Boggle board as described in the rules of Boggle.  The method
        /// is case-insensitive.
        /// </summary>
        public bool CanBeFormed(string word)
        {
            // Work in upper case
            word = word.ToUpper();

            // Mark every square on the board as unvisited.
            bool[,] visited = new bool[4, 4];

            // See if there is any starting point on the board from which
            // the word can be formed.
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (CanBeFormed(word, i, j, visited))
                    {
                        return true;
                    }
                }
            }

            // If no starting point worked, return false.
            return false;
        }


        /// <summary>
        /// Reports whether the provided word can be formed by tracking through
        /// this Boggle board by beginning at location [i,j] and avoiding any
        /// squares marked as visited.
        /// </summary>
        private bool CanBeFormed(string word, int i, int j, bool[,] visited)
        {
            // If the word is empty, report success.
            if (word.Length == 0)
            {
                return true;
            }

            // If an index is out of bounds, report failure.
            if (i < 0 || i >= 4 || j < 0 || j >= 4)
            {
                return false;
            }

            // If this square has already been visited, report failure.
            if (visited[i, j])
            {
                return false;
            }

            // If the first letter of the word doesn't match the letter on
            // this square, report failure.  Otherwise, obtain the remainder
            // of the word that we should match next.
            // (Note that Q gets special treatment.)

            char firstChar = word[0];
            string rest = word.Substring(1);

            if (firstChar != board[i, j])
            {
                return false;
            }

            // IF NO 'Q' CHECKS AT ALL, ALL WORDS MUST EXPLICITLY APPEAR ON BOARD.
            // DRAWBACK: Q AND U WON'T OFTEN APPEAR ADJACENT, THEREFORE IT WILL BE
            // VERY UNCOMMON TO BE ABLE TO SPELL WORDS WITH Q.

            // ISSUE: NON-QU WORDS NOT ALLOWED
            // ORIGINA WAY
            //if (firstChar == 'Q')
            //{
            //    if (rest.Length == 0)
            //    {
            //        return false;
            //    }
            //    if (rest[0] != 'U') 
            //    {                   
            //        return false;
            //    }
            //    rest = rest.Substring(1);
            //}

            // ISSUE: A LITERAL "QUIT" ON BOARD WON'T WORK
            // NEW WAY: If the word being played contains "QU", the first U following
            // the Q is implied and need not be adjacent to Q on the board, or even on
            // the board at all. However, an additional U (words containing QUU) must 
            // explicitly be adjacent to Q. The implied U should increase the likelihood
            // that QU words can be played since Q and U won't show up next to each other
            // very often. The word being played must still be spelled
            // out in its entirety though.
            if (firstChar == 'Q' && (rest.Length > 0))
            {
                //if (rest.Length == 0) // THIS "IF" DISALLOWS WORDS ENDING IN Q, THE && ABOVE FIXES THIS
                //{
                //    return false; 
                //}
                if (rest[0] == 'U')
                {
                    rest = rest.Substring(1);
                }
            }

            // Check if an implied 'U' is needed
            //if (firstChar == 'Q' && (rest.Length > 0))
            //{
            //    if (rest[0] == 'U')          // trying to spell something containing atleast QU
            //    {
            //        if (rest[1] == 'U')      // trying to spell something containing QUU
            //        {
            //            // if (U chain explicitly adjacent to Q == 0) // CREATE FUNCTION THAT RETURNS LENGHT OF U CHAIN
            //            //   return false; // not enough U's, can only imply 1

            //            // else if (U chain explicitly adjacent to Q == 1)
            //            //   rest = rest.Substring(1); // imply first U

            //            // if we make it here, U chain == 2, so no implying needed
            //        }
            //        else                     // trying to spell something containing only QU
            //        {
            //            // if (U chain explicitly adjacent to Q == 0)
            //            //   rest = rest.Substring(1); // imply first U

            //            // if we make it here, U chain > 0, so no implying needed
            //        }
            //    }                
            //}

            // Mark this square as visited.
            visited[i, j] = true;

            // Try to match the remainder of the word, beginning at a neighboring square.
            if (CanBeFormed(rest, i - 1, j - 1, visited)) return true;
            if (CanBeFormed(rest, i - 1, j, visited)) return true;
            if (CanBeFormed(rest, i - 1, j + 1, visited)) return true;
            if (CanBeFormed(rest, i, j - 1, visited)) return true;
            if (CanBeFormed(rest, i, j + 1, visited)) return true;
            if (CanBeFormed(rest, i + 1, j - 1, visited)) return true;
            if (CanBeFormed(rest, i + 1, j, visited)) return true;
            if (CanBeFormed(rest, i + 1, j + 1, visited)) return true;

            // We failed.  Unmark this square and return false.
            visited[i, j] = false;
            return false;
        }
    }
}

