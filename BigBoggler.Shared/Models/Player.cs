using System;
using System.IO;
using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    [DataContract]
    public class Player
    {
        [DataMember]
        public string PeerID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int GameScore { get; set; }

        [DataMember]
        public long AbsoluteScore { get; set; }

        [DataMember]
        public WordList WordList { get; set; } = new WordList();

        [DataMember]
        public bool IsReady { get; set; }

        [DataMember]
        public bool IsScoreValidated { get; set; }

        // Alias per compatibilità SignalR
        [DataMember]
        public string ID 
        { 
            get => PeerID; 
            set => PeerID = value; 
        }

        [DataMember]
        public string NickName 
        { 
            get => Name; 
            set => Name = value; 
        }

        [DataMember]
        public int Score 
        { 
            get => GameScore; 
            set => GameScore = value; 
        }

        [DataMember]
        public bool IsLocal { get; set; }

        [DataMember]
        public bool WantsDiscard { get; set; }

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

        // Metodo per ottenere i metadati (versione semplificata per SignalR)
        public PlayerMetadata GetMetadata()
        {
            return new PlayerMetadata
            {
                PeerID = this.PeerID,
                Name = this.Name,
                GameScore = this.GameScore,
                AbsoluteScore = this.AbsoluteScore,
                WordListMetadata = this.WordList.Count > 0 ? this.WordList.GetMetadata() : null
            };
        }

        public void SetMetadata(Board board, PlayerMetadata metadata)
        {
            if (metadata == null) return;

            this.PeerID = metadata.PeerID;
            this.Name = metadata.Name;
            this.GameScore = metadata.GameScore;
            this.AbsoluteScore = metadata.AbsoluteScore;

            this.WordList = new WordList();
            if (metadata.WordListMetadata != null)
            {
                this.WordList.SetMetadata(board, metadata.WordListMetadata);
            }
        }
    }

    // Classe di trasporto per metadati
    [DataContract]
    public class PlayerMetadata
    {
        [DataMember]
        public string PeerID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int GameScore { get; set; }

        [DataMember]
        public long AbsoluteScore { get; set; }

        [DataMember]
        public WordListMetadata WordListMetadata { get; set; }
    }
}