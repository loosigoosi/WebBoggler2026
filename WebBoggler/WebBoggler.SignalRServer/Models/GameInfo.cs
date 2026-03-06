using System;
using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class GameInfo
    {
        [DataMember]
        public string? ServerTimeUTC { get; set; }

        [DataMember]
        public string? RoomState { get; set; }

        [DataMember]
        public DateTime RoundStartTime { get; set; }

        [DataMember]
        public int RoundElapsedTimeMS { get; set; }

        [DataMember]
        public int RoundDurationMS { get; set; }

        [DataMember]
        public int DeadTimeAmountMS { get; set; }
    }
}
