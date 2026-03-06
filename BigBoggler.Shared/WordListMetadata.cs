namespace BigBoggler.Models
{
    public class WordListMetadata
    {
        // Contiene il percorso dei dadi come stringa di coordinate (es. "000102" per r0c0, r0c1, r0c2)
        public string[] DicesArray { get; set; }

        // Indica se la parola corrispondente nell'array sopra è un duplicato
        public bool[] DuplicatedPropertyArray { get; set; }
    }
}
