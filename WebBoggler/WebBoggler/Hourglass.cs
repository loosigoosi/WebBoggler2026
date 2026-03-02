using System;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WebBoggler
{

    public class Hourglass
    {

        public class HourglassTickEventArgs : EventArgs
        {

            private TimeSpan ElapsedTime;

            public HourglassTickEventArgs(TimeSpan elapsedTime)
            {
                ElapsedTime = elapsedTime;
            }
        }

        private DispatcherTimer _asyncTimer = new DispatcherTimer();
        private const int DefaultIntervalMs = 100;// default di 100 ms

        private DateTime _startTime;
        private DateTime _pauseTime = DateTime.MinValue;
        private DateTime _lastSecondElapsed;

        private TimeSpan _oneSecondTimeSpan = new TimeSpan(0, 0, 1);

        private bool _perpetual;
        private TimeSpan _duration;
        private bool _isRunning;

        public Hourglass()
        {
            _asyncTimer.Tick += _asyncTimer_Tick;
            _asyncTimer.Interval = new TimeSpan(0,0,0,0,DefaultIntervalMs);
            _duration = new TimeSpan(0, 0, 1, 0, 0);
            // default di 1'00"
         }

        public bool Perpetual
        {
            get
            {
                return _perpetual;
            }
            set
            {
                _perpetual = value;
            }
        }

        public TimeSpan ElapsedTime
        {
            get
            {
                if (IsRunning)
                {
                    return (DateTime.Now.ToUniversalTime() - _startTime);
                }
                else if ((_pauseTime != DateTime.MinValue))
                {
                    return (_pauseTime - _startTime);
                }
                else
                {
                    return new TimeSpan(0);
                }

            }
        }

        public double ElapsedPercent
        {
            get
            {
                if ((_startTime != DateTime.MinValue ))
                {
                    return (((double)ElapsedTime.Ticks / (double)_duration.Ticks) * 100);
                }
                else
                {
                    return 0;
                }

            }
        }

        public TimeSpan Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                _duration = value;
            }
        }

        public TimeSpan RemainingTime
        {
            get
            {
                return (Duration - ElapsedTime);
            }
        }

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                _isRunning = value;
            }
        }

        public void Run()
        {
            if (_pauseTime == DateTime.MinValue )
            {
                _startTime = DateTime.Now.ToUniversalTime();
            }
            else
            {
                _startTime = (DateTime.Now.ToUniversalTime() - (_pauseTime - _startTime));
                _pauseTime = DateTime.MinValue;
            }

            _asyncTimer.Start();
            _isRunning = true;

        }

        public void Pause()
        {
            _isRunning = false;
            _pauseTime = DateTime.Now.ToUniversalTime();
            _asyncTimer.Stop();
        }

        public void Reset()
        {
            _asyncTimer.Stop();
            _isRunning = false;
            _pauseTime = DateTime.MinValue;
        }

        public DateTime StartTimeUTC
        {
            get
            {
                return _startTime;
            }
            set
            {
                _startTime = value;
            }
        }

        public delegate void ElapsedTenthDelegate(object sender, HourglassTickEventArgs e);

        public event EventHandler<HourglassTickEventArgs> ElapsedTenth;

        protected void OnElapsedTenth(object sender, HourglassTickEventArgs e)
        {
             if (ElapsedTenth != null) ElapsedTenth(this, e);
            //DateTime now = DateTime.Now;                 
        }

        public delegate void ElapsedSecondDelegate(object sender, HourglassTickEventArgs e);

        public event EventHandler<HourglassTickEventArgs> ElapsedSecond;

        protected virtual void OnElapsedSecond(object sender, HourglassTickEventArgs e)
        {
            if (ElapsedSecond != null) ElapsedSecond(this, e);

        }

        public event EventHandler TimeExpired;

        protected virtual void OnTimeExpired()
        {
            if(TimeExpired != null) TimeExpired(this, EventArgs.Empty);

        }

        private void _asyncTimer_Tick(object sender, object e)
        {
            // to try a thread-unsafe call, comment out the line before, 
            //  and uncomment the line after
            // OnTick(New TickEventArgs(_Counter))
            if (((DateTime.Now.ToUniversalTime()  - _startTime) >= _duration) && !_perpetual)
            {
                //TimeExpired(this, null);
                OnTimeExpired(); //raise TimeExpired
                this.Reset();
            }
            else
            {
                // RaiseEvent ElapsedTenth(Me, ElapsedTime, New EventArgs)
                if (((DateTime.Now.ToUniversalTime() - _lastSecondElapsed) < _oneSecondTimeSpan))
                {
                    //ElapsedTenth(this, new HourglassTickEventArgs(ElapsedTime));
                    OnElapsedTenth(this, new HourglassTickEventArgs(ElapsedTime));     //raise Tick
                }
                else
                {
                    _lastSecondElapsed = DateTime.Now.ToUniversalTime();
                    //ElapsedSecond(this, new HourglassTickEventArgs(ElapsedTime));
                    OnElapsedSecond(this, new HourglassTickEventArgs(ElapsedTime));      //raise Tick    
                }

            }
        }

    }
}

