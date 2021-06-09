using SubCTools.Extensions;
using SubCTools.Helpers;
using System;

namespace SubCTools.Droid.Camera
{
    public class ShutterSpeed
    {
        private static double maxSpeed;
        private static int minSpeed;
        private int[] fractions;
        private int[] positiveValues;
        private long[] nanoSeconds;

        // TODO
        // Add long nanoSecond overload

        public ShutterSpeed(Fraction minShutter, Fraction maxShutter)
        {
            minSpeed = (int)minShutter.ToDouble();
            maxSpeed = maxShutter.Denominator;
            NumberOfValues = CountValues(minSpeed) + 1 + CountValues(maxSpeed);
            nanoSeconds = new long[NumberOfValues];
            fractions = new int[CountValues(maxSpeed)];
            positiveValues = new int[CountValues(minSpeed) + 2];

            BuildValues();
            UpdateNanoseconds();
        }

        public int CurrentSpeed { get; set; }

        public long Nanoseconds => nanoSeconds[CurrentSpeed];

        public int NumberOfValues { get; set; }

        private void BuildValues()
        {
            var j = 0;

            for (int i = fractions.Length - 1; i >= 0; i--)
            {
                fractions[j] = (int)((Math.Pow(2, i + 1)) - (Math.Pow(2, i + 1)) % (Convert.ToInt32(!(i > 2)) + (Convert.ToInt32(i > 2) * (5 * Math.Pow(10, (i - 3) / 4)))));
                j++;
            }

            if (positiveValues.Length > 0)
            {
                positiveValues[0] = 1;
            }

            for (int i = 0; i < positiveValues.Length - 1; i++)
            {
                positiveValues[i + 1] = (int)((Math.Pow(2, i + 1)) - (Math.Pow(2, i + 1)) % (Convert.ToInt32(!(i > 2)) + (Convert.ToInt32(i > 2) * (5 * Math.Pow(10, (i - 3) / 4)))));
            }
        }

        public void SetIndex(int value) => CurrentSpeed = value;

        /// <summary>
        /// Returns the current shutter speed as a string.
        /// </summary>
        /// <returns>Fraction</returns>
        public override string ToString()// => "1/" + fractions[CurrentSpeed];
        {
            // Convert to inline if
            // Convert to expression bodied member
            if (CurrentSpeed < fractions.Length)
            {
                return "1/" + fractions[CurrentSpeed].ToString(); ;
            }
            else
            {
                return positiveValues[CurrentSpeed - fractions.Length].ToString();
            }
        }



        /// <summary>
        /// Returns the current shutter speed as nanoseconds
        /// </summary>
        /// <returns>NanoSeconds</returns>
        private void UpdateNanoseconds()
        {
            for (int i = 0; i < NumberOfValues; i++)
            {
                if (i < fractions.Length)
                {
                    var seconds = 1 / (double)fractions[i];
                    nanoSeconds[i] = (long)(seconds * 1_000_000_000);
                }
                else
                {
                    nanoSeconds[i] = ((long)positiveValues[i - fractions.Length] * 1_000_000_000);
                }
            }
        }


        public void DecreaseShutter()
        {
            if (CurrentSpeed < NumberOfValues - 1)
            {
                CurrentSpeed++;
            }
        }

        public void IncreaseShutter()
        {
            if (CurrentSpeed > 0)
            {
                CurrentSpeed--;
            }
        }

        /// <summary>
        /// Counts how many shutter values are required.
        /// </summary>
        /// <param name="MaxShutterSpeed"></param>
        /// <returns></returns>
        private int CountValues(double value)
        {
            var count = 0;
            while (value >= 1.953125)
            {
                count++;
                value /= 2;
            }
            return count;
        }


    }
}