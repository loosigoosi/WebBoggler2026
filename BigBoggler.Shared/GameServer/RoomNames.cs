using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BigBoggler.GameServer
{
    public class RoomNames
    {
        private readonly List<string> _names = new List<string>();
        private readonly Random _rnd = new Random();

        public RoomNames()
        {
            var assembly = typeof(RoomNames).GetTypeInfo().Assembly;
            // Verifica che il nome della risorsa sia corretto nel nuovo progetto
            string resourceName = "BigBoggler.Shared.RoomNames.txt";

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                    _names.Add(line.Trim());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore caricamento RoomNames: {ex.Message}");
            }
        }

        public string Pick(List<string> excludeList)
        {
            // Se excludeList è null, inizializzala vuota per evitare errori con LINQ
            var activeExclusions = excludeList ?? new List<string>();

            // Filtriamo i nomi disponibili
            var freeNames = _names.Where(n => !activeExclusions.Contains(n)).ToList();

            if (freeNames.Any())
            {
                // In C#, Random.Next(min, max) esclude il max, quindi .Count è corretto
                int index = _rnd.Next(0, freeNames.Count);
                return freeNames[index];
            }
            else
            {
                // Fallback se i nomi nel file sono finiti o tutti occupati
                string name;
                do
                {
                    name = $"GhostWriter{_rnd.Next(0, 100000)}";
                } while (activeExclusions.Contains(name));

                return name;
            }
        }

        public void Release(string name)
        {
            // Metodo placeholder come nell'originale. 
            // In futuro potresti voler loggare il rilascio del nome.
        }
    }
}