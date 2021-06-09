using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubCTools.Droid.Listeners
{
    public abstract class IMUSensor
    {
        readonly Dictionary<SensorType, string> unitsDict = new Dictionary<SensorType, string>
        {
            { SensorType.Accelerometer, "m/s^2" },
            { SensorType.Gyroscope, "rad/s" },
            { SensorType.MagneticField, "uT" },
            { SensorType.Orientation, "degrees" },
            { SensorType.AmbientTemperature, "degrees" },
            { SensorType.Temperature, "degrees" }
        };

        public IMUSensor(SensorType type, params float[] values)
        {
            Type = type;
            Values = values;
        }

        public float[] Values { get; }

        public SensorType Type { get; }

        public string Units => unitsDict.ContainsKey(Type) ? unitsDict[Type] : string.Empty;
    }

    public abstract class StandardSensor : IMUSensor
    {
        public StandardSensor(SensorType type, params float[] values)
            : base(type, values)
        {

        }

        public float X => Values[0];

        public float Y => Values[1];

        public float Z => Values[2];

        public override string ToString() => $"{Type.ToString()},{nameof(X)},{X}{Units},{nameof(Y)},{Y}{Units},{nameof(Z)},{Z}{Units}";
    }

    public sealed class IMUAccelerometer : StandardSensor
    {
        public IMUAccelerometer(params float[] values)
            : base(SensorType.Accelerometer, values)
        {

        }
    }

    public sealed class IMUGyro : StandardSensor
    {
        public IMUGyro(params float[] values)
            : base(SensorType.Gyroscope, values)
        {

        }
    }

    public sealed class IMUMagneticField : StandardSensor
    {
        public IMUMagneticField(params float[] values)
            : base(SensorType.MagneticField, values)
        {

        }
    }

    public sealed class IMUOrientation : IMUSensor
    {
        public IMUOrientation(params float[] values) :
            base(SensorType.Orientation, values)
        {
        }
        public float Yaw => Values[0];

        public float Pitch => Values[1];

        public float Roll => Values[2];

        public override string ToString() => $"{Type.ToString()},{nameof(Yaw)},{Yaw}{Units},{nameof(Pitch)},{Pitch}{Units},{nameof(Roll)},{Roll}{Units}";

    }

    public sealed class IMUTemperature : IMUSensor
    {
        public IMUTemperature(params float[] values) :
            base(SensorType.Temperature, values)
        {
        }
    }

    public sealed class IMUAmbiantTemperature : IMUSensor
    {
        public IMUAmbiantTemperature(params float[] values) :
            base(SensorType.AmbientTemperature, values)
        {
        }
    }

    public class SensorListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly SensorManager sensorManager;

        public SensorListener(Activity activity)
        {
            sensorManager = (SensorManager)activity.GetSystemService(Context.SensorService);

            sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Orientation), SensorDelay.Game);
            sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Ui);
            sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Gyroscope), SensorDelay.Ui);
            sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.MagneticField), SensorDelay.Ui);
            //sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.AmbientTemperature), SensorDelay.Ui);
            //sensorManager.RegisterListener(this, sensorManager.GetDefaultSensor(SensorType.Temperature), SensorDelay.Ui);
        }

        public IMUAccelerometer Accelerometer { get; private set; }
        public IMUGyro Gyro { get; private set; }
        public IMUMagneticField MagneticField { get; private set; }
        public IMUOrientation Orientation { get; private set; }
        //public IMUTemperature Temperature { get; private set; }
        //public IMUAmbiantTemperature AmbiantTemperature { get; private set; }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {

        }

        public void OnSensorChanged(SensorEvent e)
        {
            var values = e.Values.ToArray();
            switch (e.Sensor.Type)
            {
                case SensorType.Accelerometer:
                    Accelerometer = new IMUAccelerometer(values);
                    break;
                case SensorType.Gyroscope:
                    Gyro = new IMUGyro(values);
                    break;
                case SensorType.MagneticField:
                    MagneticField = new IMUMagneticField(values);
                    break;
                case SensorType.Orientation:
                    Orientation = new IMUOrientation(values);
                    break;
                //case SensorType.Temperature:
                //    Temperature = new IMUTemperature(values);
                //    break;
                //case SensorType.AmbientTemperature:
                //    AmbiantTemperature = new IMUAmbiantTemperature(values);
                //    break;
                default:
                    break;
            }
        }

        public override string ToString() => string.Join("\n", new[]
        {
            Accelerometer?.ToString() ?? nameof(Accelerometer),
            Gyro?.ToString() ?? nameof(Gyro),
            MagneticField?.ToString() ?? nameof(MagneticField),
            Orientation?.ToString() ?? nameof(Orientation)
        });
    }
}