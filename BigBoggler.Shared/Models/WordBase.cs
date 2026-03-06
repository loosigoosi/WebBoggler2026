using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    [DataContract]
    public class WordBase
    {
        private LinkedList<Dice> _dicePath = new LinkedList<Dice>();

        [DataMember]
        public bool Duplicated { get; set; }

        public LinkedList<Dice> DicePath 
        { 
            get => _dicePath;
            set => _dicePath = value ?? new LinkedList<Dice>();
        }

        public string Text => string.Join("", _dicePath.Select(d => d.Letter));

        public int Score
        {
            get
            {
                int length = _dicePath.Count;
                if (length >= 8) return 11;
                if (length == 7) return 5;
                if (length == 6) return 3;
                if (length == 5) return 2;
                if (length == 4 || length == 3) return 1;
                return 0;
            }
        }

        // EVENTO per UI (Word usa questo!)
        public event EventHandler<WordChangeEventArgs> Change;

        public class WordChangeEventArgs : EventArgs
        {
            public WordBase Word { get; set; }
        }

        public void AppendDiceLast(Dice dice)
        {
            _dicePath.AddLast(dice);
            OnChange();
        }

        public void AppendDiceFirst(Dice dice)
        {
            _dicePath.AddFirst(dice);
            OnChange();
        }

        public void RemoveDice(Dice dice)
        {
            _dicePath.Remove(dice);
            OnChange();
        }

        public void Clear()
        {
            _dicePath.Clear();
            OnChange();
        }

        public bool Contains(Dice dice) => _dicePath.Contains(dice);

        public bool IsFirst(Dice dice) => _dicePath.First?.Value == dice;

        public bool IsLast(Dice dice) => _dicePath.Last?.Value == dice;

        public bool IsEmpty() => _dicePath.Count == 0;

        public WordBase Clone()
        {
            var clone = new WordBase { Duplicated = this.Duplicated };
            foreach (var d in _dicePath)
                clone._dicePath.AddLast(d);
            return clone;
        }

        public override string ToString() => Text;

        protected virtual void OnChange()
        {
            Change?.Invoke(this, new WordChangeEventArgs { Word = this });
        }
    }
}
