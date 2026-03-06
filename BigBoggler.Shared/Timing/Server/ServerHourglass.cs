using System;
using System.Threading.Tasks;

namespace BigBoggler.Timing.Server
{
    /// <summary>
    /// Hourglass specializzato per scenari server (RoomMaster, SignalR).
    /// Supporta callback async e sincronizzazione con timestamp esterni.
    /// </summary>
    public class ServerHourglass : Hourglass
    {
        private Action<TimeSpan> _onSecondElapsedCallback;
        private Func<Task> _onExpiredAsyncCallback;

        public ServerHourglass() : base()
        {
        }

        /// <summary>
        /// Configura callback async da invocare alla scadenza.
        /// Utile per scenari SignalR che richiedono Task-based APIs.
        /// </summary>
        public void OnExpiredAsync(Func<Task> callback)
        {
            _onExpiredAsyncCallback = callback;
        }

        /// <summary>
        /// Configura callback sincrono per ogni secondo (utile per logging).
        /// </summary>
        public void OnSecondElapsed(Action<TimeSpan> callback)
        {
            _onSecondElapsedCallback = callback;
        }

        protected override void OnTimeExpired()
        {
            base.OnTimeExpired();

            // Callback async per server
            if (_onExpiredAsyncCallback != null)
            {
                // Fire-and-forget pattern sicuro
                var _ = Task.Run(async () =>
                {
                    try
                    {
                        await _onExpiredAsyncCallback().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Log error
                        System.Diagnostics.Debug.WriteLine("[ServerHourglass] Error in async callback: " + ex.Message);
                    }
                });
            }
        }

        protected override void OnElapsedSecond(HourglassTickEventArgs e)
        {
            base.OnElapsedSecond(e);

            if (_onSecondElapsedCallback != null)
                _onSecondElapsedCallback(e.ElapsedTime);
        }

        /// <summary>
        /// Sincronizza il timer con un timestamp UTC esterno.
        /// Utile quando il server deve allinearsi con eventi distribuiti.
        /// </summary>
        public void SyncWithExternalTime(DateTime externalStartTimeUtc, TimeSpan targetDuration)
        {
            lock (_syncLock)
            {
                _startTime = externalStartTimeUtc;
                Duration = targetDuration;

                var elapsed = DateTime.UtcNow - _startTime;
                if (elapsed < targetDuration && !IsRunning)
                {
                    // Riprendi dal punto corretto
                    _pauseTime = DateTime.UtcNow;
                    Run();
                }
            }
        }
    }
}