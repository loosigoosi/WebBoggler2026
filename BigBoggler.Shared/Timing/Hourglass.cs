using System;
using System.Timers;

namespace BigBoggler.Timing
{
    /// <summary>
    /// Timer ad alta risoluzione per gestione countdown/stopwatch in scenari client e server.
    /// Compatibile netstandard2.0, thread-safe per uso con SignalR e OpenSilver.
    /// </summary>
    public class Hourglass : IDisposable
    {
        /// <summary>
        /// Argomenti evento per tick con informazioni sul tempo trascorso
        /// </summary>
        public class HourglassTickEventArgs : EventArgs
        {
            public TimeSpan ElapsedTime { get; }

            public HourglassTickEventArgs(TimeSpan elapsedTime)
            {
                ElapsedTime = elapsedTime;
            }
        }

        // ===== PRIVATE FIELDS =====
        private readonly Timer _internalTimer;
        private const int DEFAULT_TICK_INTERVAL_MS = 100;
        
        private DateTime _startTime = DateTime.MinValue;
        private DateTime _pauseTime = DateTime.MinValue;
        private DateTime _lastSecondElapsed = DateTime.MinValue;
        
        private readonly TimeSpan _oneSecondTimeSpan = new TimeSpan(0, 0, 1);
        private readonly object _syncLock = new object();

        private bool _perpetual;
        private TimeSpan _duration;
        private bool _isRunning;
        private bool _disposed;

        // ===== CONSTRUCTOR =====
        
        /// <summary>
        /// Crea un nuovo Hourglass con durata predefinita di 1 minuto
        /// </summary>
        public Hourglass()
        {
            _internalTimer = new Timer(DEFAULT_TICK_INTERVAL_MS);
            _internalTimer.Elapsed += OnInternalTimerElapsed;
            _internalTimer.AutoReset = true;
            
            _duration = TimeSpan.FromMinutes(1); // Default: 1 minuto
        }

        // ===== PUBLIC PROPERTIES =====

        /// <summary>
        /// Se true, il timer non si ferma mai alla scadenza
        /// </summary>
        public bool Perpetual
        {
            get { lock (_syncLock) return _perpetual; }
            set { lock (_syncLock) _perpetual = value; }
        }

        /// <summary>
        /// Tempo trascorso dall'inizio o dalla ripresa
        /// </summary>
        public TimeSpan ElapsedTime
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

        /// <summary>
        /// Percentuale di tempo trascorso (0-100)
        /// </summary>
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

        /// <summary>
        /// Durata totale del countdown
        /// </summary>
        public TimeSpan Duration
        {
            get { lock (_syncLock) return _duration; }
            set { lock (_syncLock) _duration = value; }
        }

        /// <summary>
        /// Tempo rimanente (Duration - ElapsedTime)
        /// </summary>
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

        /// <summary>
        /// Indica se il timer è attualmente in esecuzione
        /// </summary>
        public bool IsRunning
        {
            get { lock (_syncLock) return _isRunning; }
        }

        /// <summary>
        /// Ora UTC di inizio (o ripresa dopo pausa)
        /// </summary>
        public DateTime StartTimeUTC
        {
            get { lock (_syncLock) return _startTime; }
            set { lock (_syncLock) _startTime = value; }
        }

        // ===== PUBLIC METHODS =====

        /// <summary>
        /// Avvia il timer (o lo riprende dopo una pausa)
        /// </summary>
        public void Run()
        {
            lock (_syncLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(Hourglass));

                if (_pauseTime == DateTime.MinValue)
                {
                    // Prima partenza
                    _startTime = DateTime.UtcNow;
                    _lastSecondElapsed = _startTime;
                }
                else
                {
                    // Ripresa da pausa: calcola nuovo start time
                    var pauseDuration = _pauseTime - _startTime;
                    _startTime = DateTime.UtcNow - pauseDuration;
                    _pauseTime = DateTime.MinValue;
                }

                _isRunning = true;
                _internalTimer.Start();
            }
        }

        /// <summary>
        /// Mette in pausa il timer
        /// </summary>
        public void Pause()
        {
            lock (_syncLock)
            {
                if (!_isRunning) return;

                _pauseTime = DateTime.UtcNow;
                _isRunning = false;
                _internalTimer.Stop();
            }
        }

        /// <summary>
        /// Resetta il timer allo stato iniziale
        /// </summary>
        public void Reset()
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

        /// <summary>
        /// Evento sollevato ogni ~100ms mentre il timer è attivo
        /// </summary>
        public event EventHandler<HourglassTickEventArgs> ElapsedTenth;

        /// <summary>
        /// Evento sollevato ogni secondo mentre il timer è attivo
        /// </summary>
        public event EventHandler<HourglassTickEventArgs> ElapsedSecond;

        /// <summary>
        /// Evento sollevato quando la durata è scaduta (se Perpetual = false)
        /// </summary>
        public event EventHandler TimeExpired;

        // ===== PRIVATE METHODS =====

        private void OnInternalTimerElapsed(object sender, ElapsedEventArgs e)
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

                // Check scadenza
                if (elapsed >= _duration && !_perpetual)
                {
                    shouldFireExpired = true;
                    _internalTimer.Stop();
                    _isRunning = false;
                }
                else
                {
                    tickArgs = new HourglassTickEventArgs(elapsed);

                    // Check se è passato un secondo
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

            // Invoca eventi fuori dal lock per evitare deadlock
            try
            {
                if (shouldFireExpired)
                {
                    TimeExpired?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    if (shouldFireSecond)
                    {
                        ElapsedSecond?.Invoke(this, tickArgs);
                    }
                    if (shouldFireTenth)
                    {
                        ElapsedTenth?.Invoke(this, tickArgs);
                    }
                }
            }
            catch
            {
                // Protegge da eccezioni negli event handler
            }
        }

        // ===== DISPOSE PATTERN =====

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
                    _internalTimer?.Stop();
                    _internalTimer?.Dispose();
                    _disposed = true;
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