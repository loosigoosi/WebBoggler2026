using BigBoggler_Common;
using System.Runtime.Serialization;
using static BigBoggler_Common.Board;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class Board
    {
        [DataMember]
        public string? LocaleID { get; set; }

        [DataMember]
        public Dice[]? DicesVector { get; set; }

        [DataMember]
        public int WordCount { get; set; }

        [DataMember]
        public long GameSerial { get; set; }


        //--- START modifiche BigBogglerTypes
        //public Dices GetValidEntries(int row, int column, WordBase targetWord)
        //{
        //    Dices validDices = new Dices();

        //    if (targetWord.DicePath.Count > 0)
        //    {
        //        int diceRow = row;
        //        int diceCol = column;
        //        for (int i = -1; i <= 1; i++)
        //        {
        //            for (int j = -1; j <= 1; j++)
        //            {
        //                try
        //                {
        //                    validDices.Add(_dicesMatrix[diceRow + i, diceCol + j]);
        //                }
        //                catch (Exception ex)
        //                {
        //                }

        //            }
        //        }

        //        validDices.RemoveAll(dice => targetWord.Contains(dice));
        //    }
        //    return validDices;

        //}

        //public List<Dice> GetValidEntries(Dice dice, WordBase targetWord)
        //{
        //    List<Dice> validDices = new List<Dice>();

        //    if (targetWord.DicePath.Count > 0)
        //    {
        //        int diceRow = dice.Row;
        //        int diceCol = dice.Column;
        //        for (int i = -1; i <= 1; i++)
        //        {
        //            for (int j = -1; j <= 1; j++)
        //            {
        //                if (!(diceRow + i < 0 | diceRow + i > 4 | diceCol + j < 0 | diceCol + j > 4))
        //                {
        //                    validDices.Add(_dicesMatrix[diceRow + i, diceCol + j]);
        //                }
        //            }
        //        }

        //        validDices = validDices.Where(diceItem => !targetWord.Contains(diceItem)).ToList();
        //    }
        //    return validDices;
        //}

        //--- END modifiche BigBogglerTypes



        /*public void Shake()
        {
            if (DicesVector == null) return;

            var random = new Random();
            string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "L", "M", "N", "O", "P", "Qu", "R", "S", "T", "U", "V", "Z" };

            for (int i = 0; i < DicesVector.Length; i++)
            {
                DicesVector[i].Letter = letters[random.Next(letters.Length)];
                DicesVector[i].Rotation = random.Next(4) * 90;
            }
        }*/
    }
}
