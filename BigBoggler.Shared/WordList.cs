using System;
using System.Collections.Generic;
using System.Linq;

namespace BigBoggler.Models
{
    public class WordList : Dictionary<string, WordBase>
    {
        /// <summary>
        /// Calcola il punteggio totale della lista, filtrando o meno i duplicati.
        /// </summary>
        public int GetTotalScore(bool includeDuplicates = false)
        {
            return this.Values
                .Where(w => includeDuplicates || !w.Duplicated)
                .Sum(w => w.Score);
        }

        /// <summary>
        /// Estrae i metadati compressi dalla lista attuale per l'invio via SignalR.
        /// </summary>
        public WordListMetadata GetMetadata()
        {
            var metadata = new WordListMetadata
            {
                DicesArray = new string[this.Count],
                DuplicatedPropertyArray = new bool[this.Count]
            };

            int i = 0;
            foreach (var word in this.Values)
            {
                // Trasforma il DicePath (LinkedList) in una stringa di coordinate "RC"
                metadata.DicesArray[i] = string.Concat(word.DicePath.Select(d => $"{d.Row}{d.Column}"));
                metadata.DuplicatedPropertyArray[i] = word.Duplicated;
                i++;
            }

            return metadata;
        }

        /// <summary>
        /// Ricostruisce la lista di parole partendo dai metadati ricevuti e dalla board corrente.
        /// </summary>
        public void SetMetadata(Board board, WordListMetadata metadata)
        {
            this.Clear();
            if (metadata?.DicesArray == null) return;

            for (int i = 0; i < metadata.DicesArray.Length; i++)
            {
                var word = new WordBase();
                string coordsPath = metadata.DicesArray[i];

                // Legge la stringa a coppie (es: "00", "11", "22")
                for (int j = 0; j <= coordsPath.Length - 2; j += 2)
                {
                    int r = int.Parse(coordsPath[j].ToString());
                    int c = int.Parse(coordsPath[j + 1].ToString());

                    // Recupera il riferimento al dado fisico dalla Board
                    var dice = board.GetDiceAt(r, c);
                    if (dice != null)
                    {
                        word.AppendDiceLast(dice);
                    }
                }

                word.Duplicated = metadata.DuplicatedPropertyArray[i];

                // Aggiunge al dizionario usando il testo come chiave univoca (minuscola)
                string key = word.Text.ToLower();
                if (!string.IsNullOrEmpty(key) && !this.ContainsKey(key))
                {
                    this.Add(key, word);
                }
            }
        }
    }
}
