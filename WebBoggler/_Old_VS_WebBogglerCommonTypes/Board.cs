using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Xml;
using Windows.UI.Xaml;
using System.Runtime.Serialization;

namespace WebBogglerCommonTypes
{
	[DataContract(IsReference= true, Namespace = "WebBogglerCommonTypes")]
	public class Board
    {

        //>>> Board >>>>>>>>>>>>>>>>>>>>>>>
        private string _boardID;
        private readonly int _gridRank;
        private string _locale_ID_id;
        private string _language;
		private Dice[,] _dicesMatrix = new Dice[5, 5];

        public Board()
        {
            _gridRank = 5;
            //_diceArray = new Dice[gridRank, gridRank];
            //_locale_ID_id = "it-IT"; // default
        }

		[DataMember]
		public string LocaleID
        {
            get { return _locale_ID_id; }
            set { _locale_ID_id = value; }
        }

        internal Dices GetValidEntries(WordBase targetWord)
        {

			Dices validDices = new Dices();

            if (targetWord.DicePath.Count > 0)
            {
				Dices validDicesFirst = default(Dices);
				Dices validDicesLast = default(Dices);

                Dice firstDice = targetWord.DicePath.First();
                Dice lastDice = targetWord.DicePath.Last();

                validDicesFirst = GetValidEntries(firstDice.Row, firstDice.Column, targetWord);
                validDicesLast = GetValidEntries(lastDice.Row, lastDice.Column, targetWord);

                validDices = (Dices)validDicesFirst.Union(validDicesLast);

            }
            return validDices;
        }

        public Dices GetValidEntries(int row, int column, WordBase targetWord)
        {
            Dices validDices = new Dices();

            if (targetWord.DicePath.Count > 0)
            {
                int diceRow = row;
                int diceCol = column;
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        try
                        {
                            validDices.Add(_dicesMatrix[diceRow + i, diceCol + j]);
                        }
                        catch (Exception ex)
                        {
                        }

                    }
                }

                validDices.RemoveAll(dice => targetWord.Contains(dice));
            }
            return validDices;

        }

        public List<Dice> GetValidEntries(Dice dice, WordBase targetWord)
        {
            List<Dice> validDices = new List<Dice>();

            if (targetWord.DicePath.Count > 0)
            {
                int diceRow = dice.Row;
                int diceCol = dice.Column;
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (!(diceRow + i < 0 | diceRow + i > 4 | diceCol + j < 0 | diceCol + j > 4))
                        {
                            validDices.Add(_dicesMatrix[diceRow + i, diceCol + j]);
                        }
                    }
                }

                validDices = validDices.Where(diceItem => !targetWord.Contains(diceItem)).ToList();
            }
            return validDices;
        }

		[DataMember]
		public string Language
        {
            get { return _language; }
        }

		[DataMember]
		public int Rank
        {
            get { return _gridRank; }
        }


        public Dice DicesMatrix(int row, int col)
        {
			return _dicesMatrix[row, col];
		}

		[DataMember]
		public Dices DicesVector
		{
			get
			{
				Dices dices = new Dices();
				for (int i = 0; i <= 4; i++)
				{
					for (int j = 0; j <= 4; j++)
					{
						dices.Add(_dicesMatrix[i, j]);
					}
				}
				return dices;
			}
		
			set {

				for (int i = 0; i <= 4; i++)
				{
					for(int j = 0; j <= 4; j++)
					{
						_dicesMatrix[i, j] = value.GetDiceAt (i,j);
					}
				}
					
			}
		}
    }
}

