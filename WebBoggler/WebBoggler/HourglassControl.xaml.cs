using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BigBoggler.Timing.Client;

namespace WebBoggler
{
    public partial class HourglassControl : UserControl
    {
        private ClientHourglass _hourglass;
        private double _hourglassRectHeight;

        public HourglassControl()
        {
            this.InitializeComponent();

            _hourglass = new ClientHourglass
            {
                Duration = TimeSpan.FromMinutes(3)
            };

            // Aggancia callback per aggiornamenti UI
            _hourglass.OnSecondElapsed((elapsed) =>
            {
                UpdateUI();
            });

            _hourglassRectHeight = rectHourglass.Height;
            this.DataContext = _hourglass;
        }

        public void HideCover()
        {
            rectCover.Visibility = Visibility.Collapsed;
        }

        public void ShowCover()
        {
            rectCover.Visibility = Visibility.Visible;
        }

        public bool Perpetual
        {
            get => _hourglass.Perpetual;
            set => _hourglass.Perpetual = value;
        }

        public TimeSpan ElapsedTime => _hourglass.ElapsedTime;

        public double ElapsedPercent => _hourglass.ElapsedPercent;

        public TimeSpan Duration
        {
            get => _hourglass.Duration;
            set => _hourglass.Duration = value;
        }

        public TimeSpan RemainingTime => _hourglass.RemainingTime;

        public bool IsRunning
        {
            get => _hourglass.IsRunning;
            set { /* ClientHourglass gestisce IsRunning internamente */ }
        }

        public void Run()
        {
            rectHourglass.Height = _hourglassRectHeight;
            _hourglass.Run();
        }

        public void Pause()
        {
            _hourglass.Pause();
        }

        public void Reset()
        {
            rectHourglass.Height = _hourglassRectHeight;
            _hourglass.Reset();
            UpdateUI(); // Aggiorna subito UI
        }

        public DateTime StartTimeUTC
        {
            get => _hourglass.StartTimeUTC;
            set => _hourglass.StartTimeUTC = value;
        }

        private void UpdateUI()
        {
            try
            {
                rectHourglass.Height = _hourglassRectHeight * (100 - ElapsedPercent) / 100;
                TimeSpan tr = _hourglass.RemainingTime;

                textTime.Text = String.Format("{0:0}:{1:00}", tr.Minutes, tr.Seconds);

                // Se il tempo è scaduto
                if (tr.TotalSeconds <= 0)
                {
                    textTime.Text = "0:00";
                    rectHourglass.Height = 0;
                }
            }
            catch { }
        }
    }
}
