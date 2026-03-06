    using System;
using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    /// <summary>
    /// Rappresenta lo stato serializzabile di una Board (5x5).
    /// Viene usata per sincronizzare la griglia tra Server e Client via SignalR.
    /// </summary>
    [DataContract]
    public class BoardMetaData
    {
        // Usiamo array di 25 elementi (5x5)
        [DataMember]
        public byte[] DicesSelectedFace { get; set; } = new byte[25];
        
        [DataMember]
        public byte[] DicesFaceRotation { get; set; } = new byte[25];
        
        [DataMember]
        public byte[] DiceCoordsRow { get; set; } = new byte[25];
        
        [DataMember]
        public byte[] DiceCoordsCol { get; set; } = new byte[25];

        public BoardMetaData()
        {
            // Inizializzazione di default
        }
    }
}
