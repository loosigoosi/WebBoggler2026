using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class Dice
    {
        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public int Rotation { get; set; }

        [DataMember]
        public string? Letter { get; set; }

        [DataMember]
        public int Row { get; set; }

        [DataMember]
        public int Column { get; set; }
    }
}
