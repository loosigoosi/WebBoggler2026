using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    [DataContract]
    public class WordBase
    {
        private LinkedList<Dice> _path = new LinkedList<Dice>();

        [DataMember]
        public string Text => string.Concat(_path.Select(d => d.SelectedString));

        [DataMember]
        public bool Duplicated { get; set; }

        [DataMember]
        public int Score
        {
            get
            {
                int l = Text.Length;
                if (l >= 8) return 11;
                if (l == 7) return 5;
                if (l == 6) return 3;
                if (l == 5) return 2;
                if (l == 4) return 1;
                return 0;
            }
            set { } // Per deserializzazione
        }

        // Per serializzazione: espone DicePath come List invece di LinkedList
        [DataMember]
        public List<Dice> DicePathList
        {
            get => _path.ToList();
            set
            {
                _path.Clear();
                if (value != null)
                {
                    foreach (var dice in value)
                        _path.AddLast(dice);
                }
            }
        }

        // Proprietà originale (non serializzata)
        public LinkedList<Dice> DicePath => _path;

        public void AppendDiceLast(Dice d) => _path.AddLast(d);
        public void RemoveDice(Dice d) => _path.Remove(d);
        public bool Contains(Dice d) => _path.Contains(d);
        
        public WordBase Clone()
        {
            var w = new WordBase();
            foreach (var d in _path) w.AppendDiceLast(d);
            w.Duplicated = this.Duplicated;
            return w;
        }
    }
}
