using System.Runtime.Serialization;

namespace BigBoggler.Models
{
    /// <summary>
    /// Classe Word per serializzazione SignalR.
    /// Il client WebBoggler ha la propria versione estesa con metodi UI (GetPolyline, ecc.)
    /// </summary>
    [DataContract]
    public class Word : WordBase
    {
        public Word() : base()
        {
        }

        // Nessun metodo aggiuntivo - il client ha la sua classe Word locale
        // che estende WordBase con funzionalità UI
    }
}