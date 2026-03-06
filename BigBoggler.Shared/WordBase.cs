using System.Collections.Generic;
using System.Linq;

namespace BigBoggler.Models
{
    public class WordBase
    {
        private LinkedList<Dice> _path = new LinkedList<Dice>();
        public string Text => string.Concat(_path.Select(d => d.SelectedString));
        public bool Duplicated { get; set; }

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
        }

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
