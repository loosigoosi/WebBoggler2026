
namespace WebBogglerCommonTypes
{
	public partial class Player
    {
        private string _ID; //ID del giocatore
        private string _nickName;
        private int _rank; //Posizione in classifica
        private int _roundScore; //Punteggio del round in corso
        private int _record; //Record di round
        private int _totalRoundPlayed; //Numero totale di round giocati
        private int _totalWinningRound; //Totale vittorie
        private double _winPercent; //Percentuale vittorie 
        private bool _isLocal; //Giocatore locale
        private bool _isGuest; //Guest player
        private WordList _wordList;
		
		public string ID 
        {   get { return _ID; }
            set { _ID = value; }
        }
		
		public string NickName
        {   get { return _nickName; }
            set { _nickName = value; }
        }
		
		public int Rank 
        {   get { return _rank; }
            set { _rank = value; }
        }
		
		public int Score 
        {   get { return _roundScore; }
            set { _roundScore = value; }
        }
		
		public int Record 
        {   get { return _record; }
            set { _record = value; }
        }
		
		public int TotalRoundPlayed 
        {   get { return _totalRoundPlayed; }
            set { _totalRoundPlayed = value; }
        }
		
		public int TotalWinningRound 
        {   get { return _totalWinningRound; }
            set { _totalWinningRound = value; }
        }
		
		public double WinPercent 
        {   get { return _winPercent; }
            set { _winPercent = value; }
        }
		
		public bool IsLocal 
        {   get { return _isLocal; }
            set { _isLocal = value; }
        }
		
		public bool IsGuest 
        {   get { return _isGuest; }
            set { _isGuest = value; }
        }

		internal WordList WordList 
        {   get { return _wordList; }
            set { _wordList = value; }
        }


    }
}
