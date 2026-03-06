using System;
using System.IO;

namespace BigBoggler.Models
{
    public class Player
    {
        public string PeerID { get; set; }
        public string Name { get; set; }
        public int GameScore { get; set; }
        public long AbsoluteScore { get; set; }
        public WordList WordList { get; set; } = new WordList();
        public bool IsReady { get; set; }
        public bool IsScoreValidated { get; set; }

        // Metodo per ottenere i metadati (versione semplificata per SignalR)
        public PlayerMetadata GetMetadata()
        {
            return new PlayerMetadata
            {
                PeerID = this.PeerID,
                Name = this.Name,
                GameScore = this.GameScore,
                AbsoluteScore = this.AbsoluteScore,
                // Invece di serializzare in Byte[], passiamo direttamente l'oggetto WordListMetadata
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

    // Classe di trasporto per SignalR
    public class PlayerMetadata
    {
        public string PeerID { get; set; }
        public string Name { get; set; }
        public int GameScore { get; set; }
        public long AbsoluteScore { get; set; }
        public WordListMetadata WordListMetadata { get; set; }
    }
}