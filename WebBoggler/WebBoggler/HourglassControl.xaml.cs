using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WebBoggler
{

    public partial class HourglassControl : UserControl
    {

        private Hourglass _Hourglass;
        private double _hourglassRectHeight;


        public HourglassControl()
        {
            this.InitializeComponent();

            _Hourglass = new Hourglass();
            //_Hourglass.ElapsedTenth += _Hourglass_ElapsedTenth;
            _Hourglass.ElapsedSecond += _Hourglass_ElapsedSecond;
            _Hourglass.TimeExpired += _Hourglass_TimeExpired;

            _hourglassRectHeight = rectHourglass.Height;
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
            get
            {
                return _Hourglass.Perpetual;
            }
            set
            {
                _Hourglass.Perpetual  = value;
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                return _Hourglass.ElapsedTime;
            }
        }

        public double ElapsedPercent
        {
            get
            {
                return _Hourglass.ElapsedPercent;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return _Hourglass.Duration ;
            }
            set
            {
                _Hourglass.Duration = value;
            }
        }

        public TimeSpan RemainingTime
        {
            get
            {
                return _Hourglass.RemainingTime;
            }
        }

        public bool IsRunning
        {
            get
            {
                return _Hourglass.IsRunning ;
            }
            set
            {
                _Hourglass.IsRunning = value;
            }
        }

        public void Run()
        {
            var s = new TranslateTransform() { Y = 0 };
            rectHourglass.RenderTransform = s;

            _Hourglass.Run();
        }

        public void Pause()
        {
            _Hourglass.Pause();        }

        public void Reset()
        {
            var s = new TranslateTransform() { Y = 0 };
            rectHourglass.RenderTransform = s;

            _Hourglass.Reset();
        }

        public DateTime StartTimeUTC
        {
            get
            {
                return _Hourglass.StartTimeUTC;
            }
            set
            {
                _Hourglass.StartTimeUTC = value;
            }
        }

        private void _Hourglass_ElapsedSecond(object sender, Hourglass.HourglassTickEventArgs e)
        {
            try
            {
                var s = new TranslateTransform() { X = 0, Y = _hourglassRectHeight * ElapsedPercent / 100 };
                rectHourglass.RenderTransform = s;
            TimeSpan tr = ((Hourglass)sender).RemainingTime;

            textTime.Text = String.Format("{0:0}:{1:00}",  tr.Minutes, tr.Seconds);

            }
            catch { }
            finally { }



        }

        private void _Hourglass_TimeExpired(object sender, EventArgs e)
        {
            textTime.Text = "0:00";

            var s = new TranslateTransform() { Y = _hourglassRectHeight  };
            rectHourglass.RenderTransform = s;
        }
    }

}
