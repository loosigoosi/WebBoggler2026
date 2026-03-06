using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class Word
    {
        [DataMember]
        public string? Text { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember]
        public List<Dice>? DicePath { get; set; }

        [DataMember]
        public bool Duplicated { get; set; }
    }
}
