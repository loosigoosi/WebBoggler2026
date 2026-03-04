using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class Player
    {
        [DataMember]
        public string? ID { get; set; }

        [DataMember]
        public string? NickName { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember]
        public WordList? WordList { get; set; }

        [DataMember]
        public bool IsLocal { get; set; }

        [DataMember]
        public bool IsReady { get; set; }

        [DataMember]
        public int Rank { get; set; }

        [DataMember]
        public int Record { get; set; }

        [DataMember]
        public int TotalRoundPlayed { get; set; }

        [DataMember]
        public int TotalWinningRound { get; set; }

        [DataMember]
        public double WinPercent { get; set; }

        [DataMember]
        public bool IsGuest { get; set; }

        [DataMember]
        public int TotalWordsCount { get; set; }
    }
}
