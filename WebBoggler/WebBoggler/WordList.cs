using System.Collections.Generic;
using System.Linq;
using BigBoggler.Models;

namespace WebBoggler
{
    // Estende WordList da BigBoggler.Shared con metodi collection-like
    public class WordList : BigBoggler.Models.WordList
    {
        public WordList() : base()
        {
        }

        // NUOVO: Add(Word) - conversione automatica a Dictionary.Add(key, value)
        public void Add(Word word)
        {
            if (word == null || string.IsNullOrEmpty(word.Text))
                return;

            string key = word.Text.ToLower();
            if (!this.ContainsKey(key))
            {
                this.Add(key, word); // Usa base.Add(string, WordBase)
            }
        }

        // NUOVO: ElementAt per compatibilità LINQ
        public new Word ElementAt(int index)
        {
            return (Word)this.Values.ElementAt(index);
        }
    }
}
