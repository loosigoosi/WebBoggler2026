using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WebBogglerCommonTypes;

namespace WebBogglerCommonTypes
{
	public partial class Board
	{
		public static implicit operator Board (WebBogglerServer.Board mo)
		{
			var b = new WebBogglerCommonTypes.Board();
			b.LocaleID = mo.LocaleID;
			var dv = new Dices();

			foreach(var dice in mo.DicesVector)
			{
				dv.Add((Dice)dice);
			}
			b.DicesVector = dv;
            b.WordCount = mo.WordCount;
            b.GameSerial = mo.GameSerial;

			return b;
		}    
    }

    public partial class Dice
    {
        public static implicit operator WebBogglerServer.Dice(Dice mo)
        {
            var d = new WebBogglerCommonTypes.Dice();
            d.Index = mo.Index;
            d.Letter = mo.Letter;
            d.Rotation = mo.Rotation;

            return d;

        }
        public static implicit operator Dice (WebBogglerServer.Dice mo)
        {
            var d = new Dice();
            d.Index = mo.Index;
            d.Letter = mo.Letter;
            d.Rotation = mo.Rotation;

            return d;
        }
    }

	public partial class Player
	{
		public static implicit operator WebBogglerServer.Player(Player mo)
		{
			var p = new WebBogglerCommonTypes.Player();
                p.Score  = mo.Score;
                p.Record = mo.Record;
                p.Rank = mo.Rank ;
            p.NickName = mo.NickName;
            p.IsLocal = mo.IsLocal;
            p.IsGuest = mo.IsGuest;
            p.ID = mo.ID;
            p.TotalRoundPlayed = mo.TotalRoundPlayed;
            p.TotalWinningRound = mo.TotalWinningRound;
            p.WinPercent = mo.WinPercent;


            return p;
		}
	}

	public partial class Players
	{
		public static implicit operator Players(WebBogglerServer.Players mo)
		{
			if (mo == null) return null;

			var p = new Players();

			foreach (WebBogglerServer.Player item in mo.Items)
			{
				var pl = new Player();
				pl.ID = item.ID;
				pl.NickName = item.NickName;
				pl.Score = item.Score;
				pl.IsGuest = item.IsGuest;
				pl.IsLocal = item.IsLocal;
				pl.IsReady = item.IsReady;
				pl.Rank = item.Rank;
				pl.Record = item.Record;
				pl.TotalRoundPlayed = item.TotalRoundPlayed;
				pl.TotalWinningRound = item.TotalWinningRound;
				pl.WinPercent = item.WinPercent;
				pl.TotalWordsCount = item.TotalWordsCount;
				p.Add(pl);
			}

			return p;
		}
	}

    public partial class Word :WordBase
    {
        public static implicit operator Word(WebBogglerServer.Word mo)
        {
            if (mo == null) return null;

            var w = new Word();

            if (mo.DicePath != null)
            {
                foreach (var item in mo.DicePath)
                {
                    // Use the implicit conversion defined for Dice
                    w.AppendDiceLast((Dice)item);
                }
            }

            return w;
        }

        // Convert from local Word to service proxy Word
        public static implicit operator WebBogglerServer.Word(Word mo)
        {
            if (mo == null) return null;

            var svcWord = new WebBogglerServer.Word();

            if (mo.DicePath != null && mo.DicePath.Count > 0)
            {
                var arr = new System.Collections.Generic.List<WebBogglerServer.Dice>();
                foreach (var d in mo.DicePath)
                {
                    arr.Add((WebBogglerServer.Dice)d);
                }
                svcWord.DicePath = arr.ToArray();
            }

            return svcWord;
        }

    }

    public partial class WordList
	{
        public static implicit operator WordList(WebBogglerServer.WordList mo)
        {
            
            if (mo == null) return null;

            var wl = new WordList();

            //wl.Items = new List<WebBoggler.WebBogglerServer.Word>();
            foreach (WebBogglerServer.Word item in mo.Items)
            {
                wl.Add((Word)item);
            }

            return wl;
         }

        // Convert local WordList to service proxy WordList
        public static implicit operator WebBogglerServer.WordList(WordList mo)
        {
            if (mo == null) return null;

            var svc = new WebBogglerServer.WordList();
            if (mo.Count > 0)
            {
                var arr = new System.Collections.Generic.List<WebBogglerServer.Word>();
                foreach (var w in mo)
                {
                    arr.Add((WebBogglerServer.Word)w);
                }
                svc.Items = arr.ToArray();
            }

            return svc;
        }


        //public static implicit operator WebBogglerServer.WordList(WordList mo)
        //{
        //    if (mo == null) return null; // Previene crash se mo è null

        //    var w = new WebBogglerServer.Word();

        //    // Inizializziamo una lista temporanea
        //    var tempList = new System.Collections.Generic.List<WebBogglerServer.Dice>();

        //    {
        //        foreach (Word item in mo)
        //        {
                   
        //            tempList.Add((WebBogglerServer.Word)item);
        //        }
        //    }

        //    // Convertiamo in Array per soddisfare il proxy WCF
        //    w.DicePath = tempList.ToArray();

        //    return w;
        //}
    }

}
