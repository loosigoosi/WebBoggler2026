using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using BigBoggler.Lexicon;

namespace BigBoggler.Models
{
    [DataContract]
    public class Board
    {
        private readonly int _gridRank;
        private readonly List<Dice> _dices = new List<Dice>();
        private readonly Dice[,] _diceArray;
        private readonly Random _randomizer = new Random();

        [DataMember]
        public string LocaleID { get; set; }

        [DataMember]
        public long GameSerial { get; set; }

        [DataMember]
        public int WordCount { get; set; }

        // Proprietà per serializzazione SignalR
        [DataMember]
        public Dice[] DicesVector
        {
            get
            {
                var list = new List<Dice>();
                for (int i = 0; i < _gridRank; i++)
                    for (int j = 0; j < _gridRank; j++)
                        if (_diceArray[i, j] != null)
                            list.Add(_diceArray[i, j]);
                return list.ToArray();
            }
            set
            {
                // Ricostruisce l'array 2D da quello serializzato
                if (value != null)
                {
                    foreach (var dice in value)
                    {
                        if (dice.Row < _gridRank && dice.Column < _gridRank)
                            _diceArray[dice.Row, dice.Column] = dice;
                    }
                }
            }
        }

        // Proprietà non serializzate
        public int GridRank => _gridRank;
        public Dice[,] DiceArray => _diceArray;

        public Board() : this(5)
        {
        }

        public Board(int gridRank)
        {
            _gridRank = gridRank;
            _diceArray = new Dice[gridRank, gridRank];
            LocaleID = "it-IT";
        }

        // Metodo per aggiungere i dadi caricati dall'XML
        public void AddDice(Dice dice) => _dices.Add(dice);

        public void Shake()
        {
            var diceBag = new List<Dice>(_dices);
            for (int i = 0; i < _gridRank; i++)
                for (int j = 0; j < _gridRank; j++)
                {
                    if (diceBag.Count == 0) break;
                    var d = diceBag[_randomizer.Next(0, diceBag.Count)];
                    d.Randomize(); 
                    d.Row = i; 
                    d.Column = j;
                    _diceArray[i, j] = d;
                    diceBag.Remove(d);
                }
        }

        public Dice GetDiceAt(int r, int c) => 
            (r >= 0 && r < _gridRank && c >= 0 && c < _gridRank) ? _diceArray[r, c] : null;

        public WordList Solve(Lexicon.Lexicon lexicon)
        {
            var results = new WordList();
            for (int r = 0; r < _gridRank; r++)
                for (int c = 0; c < _gridRank; c++)
                    SolveRecursive(_diceArray[r, c], new WordBase(), results, lexicon);
            return results;
        }

        private void SolveRecursive(Dice current, WordBase path, WordList results, Lexicon.Lexicon lexicon)
        {
            if (path.Contains(current)) return;
            path.AppendDiceLast(current);
            string txt = path.Text.ToUpper();

            // Pruning basato sui prefissi del Lexicon
            if (txt.Length >= lexicon.IndexLength)
            {
                if (!lexicon.Words.ContainsKey(txt.Substring(0, lexicon.IndexLength)))
                {
                    path.RemoveDice(current); 
                    return;
                }
            }

            if (txt.Length >= 4 && lexicon.Validate(txt))
            {
                if (!results.ContainsKey(txt.ToLower())) 
                    results.Add(txt.ToLower(), path.Clone());
            }

            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0) continue;
                    var next = GetDiceAt(current.Row + i, current.Column + j);
                    if (next != null) SolveRecursive(next, path, results, lexicon);
                }
            path.RemoveDice(current);
        }

        public BoardMetaData GetMetaData()
        {
            var md = new BoardMetaData();
            for (int i = 0; i < _dices.Count; i++)
            {
                md.DicesSelectedFace[i] = (byte)_dices[i].SelectedFace;
                md.DicesFaceRotation[i] = (byte)(_dices[i].FaceRotation / 90);
                md.DiceCoordsRow[i] = (byte)_dices[i].Row;
                md.DiceCoordsCol[i] = (byte)_dices[i].Column;
            }
            return md;
        }
    }
}