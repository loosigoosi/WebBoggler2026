using System;
using System.Timers;

namespace BigBoggler.Timing
{
    /// <summary>
    /// Timer base thread-safe per countdown/stopwatch.
    /// Usabile sia in scenari server che client senza dipendenze UI.
    /// </summary>
    public class Hourglass : IDisposable
    {
        public class HourglassTickEventArgs : EventArgs
        {
            public TimeSpan ElapsedTime { get; private set; }

            public HourglassTickEventArgs(TimeSpan elapsedTime)
            {
                ElapsedTime = elapsedTime;
            }
        }

        // ===== FIELDS =====
        private readonly Timer _internalTimer;
        private const int DEFAULT_TICK_INTERVAL_MS = 100;
        
        protected DateTime _startTime = DateTime.MinValue;
        protected DateTime _pauseTime = DateTime.MinValue;
        protected DateTime _lastSecondElapsed = DateTime.MinValue;
        
        protected readonly TimeSpan _oneSecondTimeSpan = new TimeSpan(0, 0, 1);
        protected readonly object _syncLock = new object();

        private bool _perpetual;
        private TimeSpan _duration;
        private bool _isRunning;
        private bool _disposed;

        // ===== CONSTRUCTOR =====
        
        public Hourglass() : this(DEFAULT_TICK_INTERVAL_MS)
        {
        }

        protected Hourglass(int tickIntervalMs)
        {
            _internalTimer = new Timer(tickIntervalMs);
            _internalTimer.Elapsed += OnInternalTimerElapsed;
            _internalTimer.AutoReset = true;
            
            _duration = TimeSpan.FromMinutes(1);
        }

        // ===== PROPERTIES =====

        public bool Perpetual
        {
            get { lock (_syncLock) return _perpetual; }
            set { lock (_syncLock) _perpetual = value; }
        }

        public virtual TimeSpan ElapsedTime
        {
            get
            {
                lock (_syncLock)
                {
                    if (_isRunning)
                    {
                        return DateTime.UtcNow - _startTime;
                    }
                    else if (_pauseTime != DateTime.MinValue)
                    {
                        return _pauseTime - _startTime;
                    }
                    else
                    {
                        return TimeSpan.Zero;
                    }
                }
            }
        }

        public double ElapsedPercent
        {
            get
            {
                lock (_syncLock)
                {
                    if (_startTime == DateTime.MinValue || _duration.Ticks == 0)
                        return 0;

                    var elapsed = ElapsedTime.Ticks;
                    var total = _duration.Ticks;
                    return Math.Min(100.0, (double)elapsed / total * 100.0);
                }
            }
        }

        public TimeSpan Duration
        {
            get { lock (_syncLock) return _duration; }
            set { lock (_syncLock) _duration = value; }
        }

        public TimeSpan RemainingTime
        {
            get
            {
                lock (_syncLock)
                {
                    if (!_isRunning)
                        return TimeSpan.Zero;

                    var remaining = _duration - ElapsedTime;
                    return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                }
            }
        }

        public bool IsRunning
        {
            get { lock (_syncLock) return _isRunning; }
        }

        public DateTime StartTimeUTC
        {
            get { lock (_syncLock) return _startTime; }
            set { lock (_syncLock) _startTime = value; }
        }

        // ===== METHODS =====

        public virtual void Run()
        {
            lock (_syncLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Hourglass));

                if (_pauseTime == DateTime.MinValue)
                {
                    _startTime = DateTime.UtcNow;
                    _lastSecondElapsed = _startTime;
                }
                else
                {
                    var pauseDuration = _pauseTime - _startTime;
                    _startTime = DateTime.UtcNow - pauseDuration;
                    _pauseTime = DateTime.MinValue;
                }

                _isRunning = true;
                _internalTimer.Start();
            }
        }

        public virtual void Pause()
        {
            lock (_syncLock)
            {
                if (!_isRunning) return;

                _pauseTime = DateTime.UtcNow;
                _isRunning = false;
                _internalTimer.Stop();
            }
        }

        public virtual void Reset()
        {
            lock (_syncLock)
            {
                _internalTimer.Stop();
                _isRunning = false;
                _pauseTime = DateTime.MinValue;
                _startTime = DateTime.MinValue;
                _lastSecondElapsed = DateTime.MinValue;
            }
        }

        // ===== EVENTS =====

        public event EventHandler<HourglassTickEventArgs> ElapsedTenth;
        public event EventHandler<HourglassTickEventArgs> ElapsedSecond;
        public event EventHandler TimeExpired;

        // ===== PROTECTED VIRTUAL METHODS =====

        protected virtual void OnInternalTimerElapsed(object sender, ElapsedEventArgs e)
        {
            bool shouldFireExpired = false;
            bool shouldFireSecond = false;
            bool shouldFireTenth = false;
            HourglassTickEventArgs tickArgs = null;

            lock (_syncLock)
            {
                if (!_isRunning) return;

                var now = DateTime.UtcNow;
                var elapsed = now - _startTime;

                if (elapsed >= _duration && !_perpetual)
                {
                    shouldFireExpired = true;
                    _internalTimer.Stop();
                    _isRunning = false;
                }
                else
                {
                    tickArgs = new HourglassTickEventArgs(elapsed);

                    if ((now - _lastSecondElapsed) >= _oneSecondTimeSpan)
                    {
                        _lastSecondElapsed = now;
                        shouldFireSecond = true;
                    }
                    else
                    {
                        shouldFireTenth = true;
                    }
                }
            }

            RaiseEvents(shouldFireExpired, shouldFireSecond, shouldFireTenth, tickArgs);
        }

        protected virtual void RaiseEvents(bool fireExpired, bool fireSecond, bool fireTenth, HourglassTickEventArgs args)
        {
            try
            {
                if (fireExpired)
                {
                    OnTimeExpired();
                }
                else
                {
                    if (fireSecond)
                        OnElapsedSecond(args);
                    if (fireTenth)
                        OnElapsedTenth(args);
                }
            }
            catch
            {
                // Protegge da eccezioni negli event handler
            }
        }

        protected virtual void OnTimeExpired()
        {
            var handler = TimeExpired;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected virtual void OnElapsedSecond(HourglassTickEventArgs e)
        {
            var handler = ElapsedSecond;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnElapsedTenth(HourglassTickEventArgs e)
        {
            var handler = ElapsedTenth;
            if (handler != null)
                handler(this, e);
        }

        // ===== DISPOSE =====

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                lock (_syncLock)
                {
                    if (_internalTimer != null)
                    {
                        _internalTimer.Stop();
                        _internalTimer.Dispose();
                    }
                }
            }

            _disposed = true;
        }

        ~Hourglass()
        {
            Dispose(false);
        }
    }
}                                                                                                   