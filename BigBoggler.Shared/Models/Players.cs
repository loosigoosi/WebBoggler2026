using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    /// <summary>
    /// Collezione di Player per SignalR e gestione giocatori
    /// </summary>
    [DataContract]
    public class Players : List<Player>
    {
        public Players() : base()
        {
        }

        /// <summary>
        /// Array di player per serializzazione SignalR (compatibilità DTO)
        /// </summary>
        [DataMember]
        public Player[] Items
        {
            get => this.ToArray();
            set
            {
                this.Clear();
                if (value != null)
                    this.AddRange(value);
            }
        }
    }
}