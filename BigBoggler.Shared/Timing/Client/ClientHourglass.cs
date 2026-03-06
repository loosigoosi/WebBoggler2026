using System;
using System.ComponentModel;

namespace BigBoggler.Timing.Client
{
    /// <summary>
    /// Hourglass specializzato per binding UI (XAML/WPF/OpenSilver).
    /// Implementa INotifyPropertyChanged per aggiornamenti automatici dell'interfaccia.
    /// </summary>
    public class ClientHourglass : Hourglass, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ClientHourglass() : base()
        {
            // Subscribe agli eventi base per notificare le properties
            ElapsedSecond += OnElapsedSecondInternal;
        }

        private void OnElapsedSecondInternal(object sender, HourglassTickEventArgs e)
        {
            NotifyPropertiesChanged();
        }

        // ===== PROPERTIES PER BINDING UI =====

        /// <summary>
        /// Formato stringa "mm:ss" per TextBlock binding
        /// </summary>
        public string RemainingTimeFormatted
        {
            get
            {
                var time = RemainingTime;
                return string.Format("{0}:{1:00}", (int)time.TotalMinutes, time.Seconds);
            }
        }

        /// <summary>
        /// Percentuale rimanente (inverso di ElapsedPercent) per ProgressBar
        /// </summary>
        public double RemainingPercent
        {
            get { return 100.0 - ElapsedPercent; }
        }

        // ===== OVERRIDE CON NOTIFICHE =====

        public override void Run()
        {
            base.Run();
            NotifyPropertiesChanged();
        }

        public override void Pause()
        {
            base.Pause();
            NotifyPropertiesChanged();
        }

        public override void Reset()
        {
            base.Reset();
            NotifyPropertiesChanged();
        }

        protected override void OnElapsedSecond(HourglassTickEventArgs e)
        {
            base.OnElapsedSecond(e);
            NotifyPropertiesChanged();
        }

        private void NotifyPropertiesChanged()
        {
            OnPropertyChanged("ElapsedTime");
            OnPropertyChanged("ElapsedPercent");
            OnPropertyChanged("RemainingTime");
            OnPropertyChanged("RemainingTimeFormatted");
            OnPropertyChanged("RemainingPercent");
            OnPropertyChanged("IsRunning");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}