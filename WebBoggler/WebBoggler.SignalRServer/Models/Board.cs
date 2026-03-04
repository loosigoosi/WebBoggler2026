using System.Runtime.Serialization;

namespace WebBoggler.SignalRServer.Models
{
    [DataContract]
    public class Board
    {
        [DataMember]
        public string? LocaleID { get; set; }

        [DataMember]
        public Dice[]? DicesVector { get; set; }

        [DataMember]
        public int WordCount { get; set; }

        [DataMember]
        public long GameSerial { get; set; }

        public void Shake()
        {
            if (DicesVector == null) return;

            var random = new Random();
            string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "L", "M", "N", "O", "P", "Qu", "R", "S", "T", "U", "V", "Z" };

            for (int i = 0; i < DicesVector.Length; i++)
            {
                DicesVector[i].Letter = letters[random.Next(letters.Length)];
                DicesVector[i].Rotation = random.Next(4) * 90;
            }
        }
    }
}
