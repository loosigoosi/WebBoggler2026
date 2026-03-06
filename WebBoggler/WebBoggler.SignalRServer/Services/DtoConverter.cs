using System;
using System.Collections.Generic;
using System.Linq;
using SharedModels = BigBoggler.Models;  // Alias per i modelli shared
using LegacyCommon = BigBoggler_Common;   // Alias per la DLL legacy
using DtoModels = WebBoggler.SignalRServer.Models;  // Alias per i DTO locali

namespace WebBoggler.SignalRServer.Services
{
    /// <summary>
    /// Converte tra BigBoggler_Common (DLL legacy), BigBoggler.Models (shared) 
    /// e WebBoggler.SignalRServer.Models (DTO legacy locali)
    /// Temporaneo: permette migrazione graduale
    /// </summary>
    public static class DtoConverter
    {
        /// <summary>
        /// Converte Board legacy (BigBoggler_Common) in DTO locale
        /// </summary>
        public static DtoModels.Board ToLegacyDto(LegacyCommon.Board source)
        {
            if (source == null) return null;

            var dto = new DtoModels.Board
            {
                LocaleID = source.LocaleID,
                GameSerial = source.BoardID,
                DicesVector = new DtoModels.Dice[source.GridRank * source.GridRank]
            };

            int index = 0;
            for (int row = 0; row < source.GridRank; row++)
            {
                for (int col = 0; col < source.GridRank; col++)
                {
                    var srcDice = source.DiceArray[row, col];
                    dto.DicesVector[index] = new DtoModels.Dice
                    {
                        Index = index,
                        Letter = srcDice.Letter,
                        Rotation = srcDice.Rotation
                    };
                    index++;
                }
            }

            return dto;
        }

        /// <summary>
        /// Converte Board legacy in modello shared (futuro)
        /// </summary>
        public static SharedModels.Board ToSharedModel(LegacyCommon.Board source)
        {
            if (source == null) return null;

            var shared = new SharedModels.Board
            {
                LocaleID = source.LocaleID,
                GameSerial = source.BoardID,
                DicesVector = new SharedModels.Dice[source.GridRank * source.GridRank]
            };

            int index = 0;
            for (int row = 0; row < source.GridRank; row++)
            {
                for (int col = 0; col < source.GridRank; col++)
                {
                    var srcDice = source.DiceArray[row, col];
                    shared.DicesVector[index] = new SharedModels.Dice
                    {
                        Index = index,
                        Letter = srcDice.Letter,
                        Rotation = srcDice.Rotation
                    };
                    index++;
                }
            }

            return shared;
        }

        /// <summary>
        /// Converte WordList legacy in DTO locale
        /// </summary>
        public static DtoModels.WordList ToLegacyDto(LegacyCommon.WordList source)
        {
            if (source == null || source.Count == 0)
                return new DtoModels.WordList { Items = Array.Empty<DtoModels.Word>() };

            var words = new List<DtoModels.Word>();

            foreach (var srcWord in source)
            {
                var dtoWord = new DtoModels.Word
                {
                    DicePath = new List<DtoModels.Dice>(),
                    Duplicated = false
                };

                foreach (var srcDice in srcWord.DicePath)
                {
                    dtoWord.DicePath.Add(new DtoModels.Dice
                    {
                        Index = srcDice.Index,
                        Letter = srcDice.Letter,
                        Rotation = srcDice.Rotation
                    });
                }

                dtoWord.Score = CalculateScore(dtoWord.DicePath.Count);
                words.Add(dtoWord);
            }

            return new DtoModels.WordList { Items = words.ToArray() };
        }

        /// <summary>
        /// Converte WordList legacy in modello shared (futuro)
        /// </summary>
        public static SharedModels.WordList ToSharedModel(LegacyCommon.WordList source)
        {
            if (source == null || source.Count == 0)
                return new SharedModels.WordList();

            var shared = new SharedModels.WordList();

            foreach (var srcWord in source)
            {
                var sharedWord = new SharedModels.Word();

                foreach (var srcDice in srcWord.DicePath)
                {
                    sharedWord.AppendDice(new SharedModels.Dice
                    {
                        Index = srcDice.Index,
                        Letter = srcDice.Letter,
                        Rotation = srcDice.Rotation
                    });
                }

                shared.Add(sharedWord);
            }

            return shared;
        }

        private static int CalculateScore(int length)
        {
            if (length >= 8) return 11;
            if (length == 7) return 5;
            if (length == 6) return 3;
            if (length == 5) return 2;
            if (length == 4 || length == 3) return 1;
            return 0;
        }
    }
}