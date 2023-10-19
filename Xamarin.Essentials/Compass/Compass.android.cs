using System;
using System.Diagnostics;
using Android.Hardware;
using Android.Runtime;

namespace Xamarin.Essentials
{
    public static partial class Compass
    {
        internal static bool IsSupported =>
            Platform.SensorManager?.GetDefaultSensor(SensorType.Accelerometer) != null &&
            Platform.SensorManager?.GetDefaultSensor(SensorType.MagneticField) != null;

        static SensorListener listener;
        static Sensor magnetometer;
        static Sensor accelerometer;

        internal static void PlatformStart(SensorSpeed sensorSpeed, bool applyLowPassFilter)
        {
            var delay = sensorSpeed.ToPlatform();
            accelerometer = Platform.SensorManager.GetDefaultSensor(SensorType.Accelerometer);
            magnetometer = Platform.SensorManager.GetDefaultSensor(SensorType.MagneticField);
            listener = new SensorListener(accelerometer.Name, magnetometer.Name, delay, applyLowPassFilter);
            Platform.SensorManager.RegisterListener(listener, accelerometer, delay);
            Platform.SensorManager.RegisterListener(listener, magnetometer, delay);
        }

        internal static void PlatformStop()
        {
            if (listener == null)
                return;

            Platform.SensorManager.UnregisterListener(listener, accelerometer);
            Platform.SensorManager.UnregisterListener(listener, magnetometer);
            listener.Dispose();
            listener = null;
        }
    }

    class SensorListener : Java.Lang.Object, ISensorEventListener, IDisposable
    {
        LowPassFilter filter = new LowPassFilter();
        float[] lastAccelerometer = new float[3];
        float[] lastMagnetometer = new float[3];
        bool lastAccelerometerSet;
        bool lastMagnetometerSet;
        float[] r = new float[9];
        float[] orientation = new float[3];

        string magnetometer;
        string accelerometer;
        bool applyLowPassFilter;

        internal SensorListener(string accelerometer, string magnetometer, SensorDelay delay, bool applyLowPassFilter)
        {
            this.magnetometer = magnetometer;
            this.accelerometer = accelerometer;
            this.applyLowPassFilter = applyLowPassFilter;
        }

        void ISensorEventListener.OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
        }

        void ISensorEventListener.OnSensorChanged(SensorEvent e)
        {
            if (e.Sensor.Name == accelerometer && !lastAccelerometerSet)
            {
                var modifiedValues = new float[3];
                e.Values.CopyTo(modifiedValues, 0);
                modifiedValues[2] = Math.Abs(modifiedValues[2]);
                modifiedValues.CopyTo(lastAccelerometer, 0);
                Debug.WriteLine($"accelerometer: {e.Values[0]} {e.Values[1]} {e.Values[2]}\n");
                Debug.WriteLine($"modified: {modifiedValues[0]} {modifiedValues[1]} {modifiedValues[2]}\n");
                lastAccelerometerSet = true;
            }
            else if (e.Sensor.Name == magnetometer && !lastMagnetometerSet)
            {
                e.Values.CopyTo(lastMagnetometer, 0);
                lastMagnetometerSet = true;
            }

            if (lastAccelerometerSet && lastMagnetometerSet)
            {
                SensorManager.GetRotationMatrix(r, null, lastAccelerometer, lastMagnetometer);
                SensorManager.GetOrientation(r, orientation);

                if (orientation.Length <= 0)
                    return;

                var azimuthInRadians = orientation[0];
                if (applyLowPassFilter)
                {
                    filter.Add(azimuthInRadians);
                    azimuthInRadians = filter.Average();
                }
                var azimuthInDegress = (Java.Lang.Math.ToDegrees(azimuthInRadians) + 360.0) % 360.0;

                var data = new CompassData(azimuthInDegress);
                Compass.OnChanged(data);
                lastMagnetometerSet = false;
                lastAccelerometerSet = false;
            }
        }
    }
}
