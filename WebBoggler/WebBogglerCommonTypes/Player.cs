using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebBogglerCommonTypes
{
	[DataContract(IsReference = true, Namespace = "WebBogglerCommonTypes")]
	public class Player
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
		private bool _isReady; //Giocatore pronto per iniziare
		private int _totalWordsCount; //Totale parole trovate

		[DataMember]
		public string ID 
        {   get { return _ID; }
            set { _ID = value; }
        }
		[DataMember]
		public string NickName
        {   get { return _nickName; }
            set { _nickName = value; }
        }
		[DataMember]
		public int Rank 
        {   get { return _rank; }
            set { _rank = value; }
        }
		[DataMember]
		public int Score 
        {   get { return _roundScore; }
            set { _roundScore = value; }
        }
		[DataMember]
		public int Record 
        {   get { return _record; }
            set { _record = value; }
        }
		[DataMember]
		public int TotalRoundPlayed 
        {   get { return _totalRoundPlayed; }
            set { _totalRoundPlayed = value; }
        }
		[DataMember]
		public int TotalWinningRound 
        {   get { return _totalWinningRound; }
            set { _totalWinningRound = value; }
        }
		[DataMember]
		public double WinPercent 
        {   get { return _winPercent; }
            set { _winPercent = value; }
        }
		[DataMember]
		public bool IsLocal 
        {   get { return _isLocal; }
            set { _isLocal = value; }
        }
			[DataMember]
			public bool IsGuest 
			{   get { return _isGuest; }
				set { _isGuest = value; }
			}
			[DataMember]
			public bool IsReady 
			{   get { return _isReady; }
				set { _isReady = value; }
			}
			[DataMember]
			public int TotalWordsCount 
			{   get { return _totalWordsCount; }
				set { _totalWordsCount = value; }
			}
			}
		}
