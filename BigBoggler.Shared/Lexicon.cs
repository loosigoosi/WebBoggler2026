using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BigBoggler.Models
{
    public class Lexicon
    {
        private readonly Dictionary<string, HashSet<string>> _lex = new Dictionary<string, HashSet<string>>();
        public int IndexLength { get; }

        public Lexicon(string lang, int min = 4)
        {
            IndexLength = min;
            var assembly = typeof(Lexicon).GetTypeInfo().Assembly;
            using (var s = assembly.GetManifestResourceStream($"BigBoggler.Resources.lexicon_{lang}.txt"))
            using (var r = new StreamReader(s))
            {
                string w;
                while ((w = r.ReadLine()) != null)
                {
                    w = w.Trim().ToUpper();
                    if (w.Length < min) continue;
                    string pre = w.Substring(0, min);
                    if (!_lex.ContainsKey(pre)) _lex[pre] = new HashSet<string>();
                    _lex[pre].Add(w);
                }
            }
        }
        public bool Validate(string t) =>
            _lex.TryGetValue(t.ToUpper().Substring(0, IndexLength), out var set) && set.Contains(t.ToUpper());
        public Dictionary<string, HashSet<string>> Words => _lex;
    }
}
