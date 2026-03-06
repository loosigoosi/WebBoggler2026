using System;
using System.IO;
using System.Reflection;

namespace BigBoggler.Models
{
    public interface IWavPlayer
    {
        void StartPlaySound(Stream soundStream, string soundName);
    }

    public class SoundPlayer
    {
        public enum Sound
        {
            LetterTap, StartGame, EndGame, AddWord, Failure,
            FailureLong, SuccessLong, ShakeBoard, Laser, Tail
        }

        private readonly IWavPlayer _wavPlayer;
        public bool Mute { get; set; } = false;

        public SoundPlayer(IWavPlayer wavPlayer)
        {
            _wavPlayer = wavPlayer;
        }

        public void PlaySound(Sound sound)
        {
            if (Mute || _wavPlayer == null) return;

            string resourceName = GetResourceName(sound);
            var assembly = typeof(SoundPlayer).GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    // Passiamo anche il nome per permettere al client di fare caching
                    _wavPlayer.StartPlaySound(stream, sound.ToString());
                }
            }
        }

        private string GetResourceName(Sound sound)
        {
            string baseRes = "BigBoggler.Shared.Resources."; // Verifica il tuo path
            switch (sound)
            {
                case Sound.LetterTap: return baseRes + "button-tick.wav";
                case Sound.StartGame: return baseRes + "1-tone-chime.wav";
                case Sound.EndGame: return baseRes + "2-tone-chime.wav";
                case Sound.AddWord: return baseRes + "add-word.wav";
                case Sound.Failure: return baseRes + "fail-short.wav";
                case Sound.FailureLong: return baseRes + "failure.wav";
                case Sound.SuccessLong: return baseRes + "harmony.wav";
                case Sound.ShakeBoard: return baseRes + "shaking-pillbottle.wav";
                case Sound.Laser: return baseRes + "laser.wav";
                case Sound.Tail: return baseRes + "tail.wav";
                default: return string.Empty;
            }
        }
    }
}