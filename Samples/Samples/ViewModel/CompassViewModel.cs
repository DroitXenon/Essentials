using System;
using System.Collections.Generic;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Samples.ViewModel
{
    class CompassViewModel : BaseViewModel
    {
        bool compass1IsActive;
        bool applyLowPassFilter;
        double compass1;
        int speed1 = 0;

        public CompassViewModel()
        {
            StartCompass1Command = new Command(OnStartCompass1);
            StopCompass1Command = new Command(OnStopCompass1);
        }

        public ICommand StartCompass1Command { get; }

        public ICommand StopCompass1Command { get; }

        public bool Compass1IsActive
        {
            get => compass1IsActive;
            set => SetProperty(ref compass1IsActive, value);
        }

        public bool ApplyLowPassFilter
        {
            get => applyLowPassFilter;
            set
            {
                SetProperty(ref applyLowPassFilter, value);
            }
        }

        public double Compass1
        {
            get => compass1;
            set => SetProperty(ref compass1, value);
        }

        public int Speed1
        {
            get => speed1;
            set => SetProperty(ref speed1, value);
        }

        public string[] Speeds { get; } =
           Enum.GetNames(typeof(SensorSpeed));

        public override void OnDisappearing()
        {
            OnStopCompass1();

            base.OnDisappearing();
        }

        async void OnStartCompass1()
        {
            try
            {
                Compass.Start((SensorSpeed)Speed1, ApplyLowPassFilter);
                Compass.ReadingChanged += OnCompass1ReadingChanged;
                Compass1IsActive = true;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync($"Unable to start compass: {ex.Message}");
            }
        }

        void OnCompass1ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            switch ((SensorSpeed)Speed1)
            {
                case SensorSpeed.Fastest:
                case SensorSpeed.Game:
                    MainThread.BeginInvokeOnMainThread(() => { Compass1 = e.Reading.HeadingMagneticNorth; });
                    break;
                default:
                    Compass1 = e.Reading.HeadingMagneticNorth;
                    break;
            }
        }

        void OnStopCompass1()
        {
            Compass1IsActive = false;
            Compass.Stop();
            Compass.ReadingChanged -= OnCompass1ReadingChanged;
        }
    }
}
