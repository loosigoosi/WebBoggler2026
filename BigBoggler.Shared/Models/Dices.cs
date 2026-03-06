using System.Collections.Generic;

namespace BigBoggler.Models
{
    /// <summary>
    /// Collezione helper per gestire gruppi di Dice.
    /// Non serializzato direttamente (SignalR/DataContract usano array).
    /// </summary>
    public class Dices : List<Dice>
    {
        public Dices() : base()
        {
        }

        /// <summary>
        /// Ottiene il dado alle coordinate specificate (assume griglia 5x5)
        /// </summary>
        public Dice GetDiceAt(int row, int col)
        {
            int index = row * 5 + col;
            if (index >= 0 && index < this.Count)
                return this[index];
            return null;
        }

        /// <summary>
        /// Ottiene il dado per indice lineare
        /// </summary>
        public Dice GetDiceByIndex(int index)
        {
            if (index >= 0 && index < this.Count)
                return this[index];
            return null;
        }
    }
}