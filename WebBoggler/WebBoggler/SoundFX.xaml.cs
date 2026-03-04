using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WebBoggler
{
    public enum Sound
    {
        LetterTap,
        StartGame,
        EndGame,
        AddWord,
        Failure,
        ShakeBoard,
        Erase
    }

    public partial class SoundFX : UserControl
    {

        private MediaElement _letterTap;
        private MediaElement _startGame;
        private MediaElement _endGame;
        private MediaElement _addWord;
        private MediaElement _failure;
        private MediaElement _shakeBoard;
        private MediaElement _laser;
        public bool Mute = false;

        public SoundFX()
        {
            this.InitializeComponent();



        }

        public void PlaySound(Sound sound)
        {
            _letterTap = wavButtonTick;
            _startGame = wavStartChime;
            _endGame = wavEndChime;
            _addWord = wavAddWord;
            _failure = wavFail;
            _shakeBoard = wavShake;
            _laser = wavErase;

            MediaElement mediaToPlay = null;
            switch (sound)
            {
                case Sound.LetterTap:
                    mediaToPlay = _letterTap;
                    break;
                case Sound.StartGame:
                    mediaToPlay = _startGame;
                    break;
                case Sound.EndGame:
                    mediaToPlay = _endGame;
                    break;
                case Sound.AddWord:
                    mediaToPlay = _addWord;
                    break;
                case Sound.Failure:
                    mediaToPlay = _failure;
                    break;
                case Sound.ShakeBoard:
                    mediaToPlay = _shakeBoard;
                    break;
                case Sound.Erase:
                    mediaToPlay = _laser;
                    break;
            }

            if (!Mute)
            {
                try
                {
                    if (mediaToPlay != null)
                    {
                        mediaToPlay.Stop(); // Ferma se sta già suonando
                        mediaToPlay.Play(); // Riproduci il suono
                    }
                }
                catch { }
            }
        }
    }
}
