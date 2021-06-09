// <copyright file="Numbers.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Numbers
    {
        /// <summary>
        /// Convert bits to bytes.
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static double BitsToBytes(double bits)
        {
            return bits / 8;
        }

        /// <summary>
        /// Convert bytes to GB.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static long BytesToGB(long bytes)
        {
            return BytesToMB(bytes) / 1024;
        }

        public static double BytesToGB(double bytes)
        {
            return BytesToMB(bytes) / 1024;
        }

        /// <summary>
        /// Convert bytes to KB.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static long BytesToKB(long bytes)
        {
            return bytes / 1024;
        }

        public static double BytesToKB(double bytes)
        {
            return bytes / 1024;
        }

        /// <summary>
        /// Convert bytes to MB.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static long BytesToMB(long bytes)
        {
            return BytesToKB(bytes) / 1024;
        }

        public static double BytesToMB(double bytes)
        {
            return BytesToKB(bytes) / 1024;
        }

        /// <summary>
        /// Maps a number in the first range into an equivalent number in the second range.
        /// </summary>
        /// <param name="value"> The value to be mapped.</param>
        /// <param name="range1Min"> The min value of the first range.</param>
        /// <param name="range1Max"> The max value of the first range.</param>
        /// <param name="range2Min"> The min value of the second range.</param>
        /// <param name="range2Max"> The max value of the second range.</param>
        /// <returns></returns>
        public static double ConvertRange(double value, double range1Min = 0, double range1Max = 100, double range2Min = 0, double range2Max = 255)
        {
            var m = (range2Min - range2Max) / (range1Min - range1Max);
            return m * (value - range1Min) + range2Min;
        }

        /// <summary>
        /// Convert the input number to milliwatts. ie. 10 returns 0.01.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static decimal ConvertToMW(int number)
        {
            return number * 0.001m;
        }

        public static double FractionToDouble(string fraction)
        {
            if (double.TryParse(fraction, out var result))
            {
                return result;
            }

            var split = fraction.Split(new char[] { ' ', '/' });

            if (split.Length == 2 || split.Length == 3)
            {
                if (int.TryParse(split[0], out var a) && int.TryParse(split[1], out var b))
                {
                    if (split.Length == 2)
                    {
                        return (double)a / b;
                    }

                    if (int.TryParse(split[2], out var c))
                    {
                        return a + (double)b / c;
                    }
                }
            }

            throw new FormatException("Not a valid fraction.");
        }

        public static long GBToBytes(long bytes)
        {
            return MBToBytes(bytes) * 1024;
        }

        public static double GBToBytes(double bytes)
        {
            return MBToBytes(bytes) * 1024;
        }

        public static int GetLowestExcludedInt(IEnumerable<int> ints, int lowBound = 0)
        {
            return System.Linq.Enumerable.Range(lowBound, ints.Count()).Except(ints).DefaultIfEmpty(ints.Count()).First();
        }

        public static double HoursToSeconds(double hours)
        {
            return hours * 60 * 60;
        }

        /// <summary>
        /// Determine if a value is an Int32.
        /// </summary>
        /// <param name="value">Value to test.</param>
        /// <returns>Whether the value is an int or not.</returns>
        public static bool IsInt(object value)
        {
            try
            {
                Convert.ToInt32(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static double KBitsToBytes(double kBits)
        {
            return BitsToBytes(kBits * 1024);
        }

        public static long KBToBytes(long bytes)
        {
            return bytes * 1024;
        }

        public static double KBToBytes(double bytes)
        {
            return bytes * 1024;
        }

        public static double MBitsPerSecondToGBPerHour(double mBits)
        {
            return mBits * 1_000_000d * Math.Pow(60, 2) / 8d / Math.Pow(1024, 3);
        }

        public static double MBitsToBytes(double mBits)
        {
            return KBitsToBytes(mBits * 1024);
        }

        public static long MBToBytes(long bytes)
        {
            return KBToBytes(bytes) * 1024;
        }

        public static double MBToBytes(double bytes)
        {
            return KBToBytes(bytes) * 1024;
        }

        /// <summary>
        /// Get the Millisecond to Step equivilent. 3ms = 1 step.
        /// </summary>
        /// <param name="timeMS">Amount of time in MS.</param>
        /// <returns>3ms/step equivilent.</returns>
        public static int MSToStep(int timeMS)
        {
            return Convert.ToInt32(Math.Round((double)timeMS / 3));
        }

        public static double NanoToSeconds(this double value)
        {
            return value / 1000000000;
        }

        public static long NanoToSeconds(this long value)
        {
            return value / 1000000000;
        }

        /// <summary>
        /// Converts a 0-100 percentage in to a 0-255 step value.
        /// </summary>
        /// <param name="percent">0%-100% Percentage.</param>
        /// <returns>0-255 step range.</returns>
        public static int PercentToStep(int percent)
        {
            return Convert.ToInt32(Math.Round(percent / 100D * 255));
        }

        public static int RoundPlace(decimal num)
        {
            var strNum = num.ToString();

            if (!strNum.Contains("."))
            {
                return 0;
            }

            var isCounting = false;
            var place = 0;

            foreach (var c in strNum)
            {
                if (isCounting)
                {
                    place++;

                    if (c != '0')
                    {
                        break;
                    }
                }

                if (c == '.')
                {
                    isCounting = true;
                }
            }

            return place;
        }

        public static double SecondsToHours(double sec)
        {
            return sec / 60 / 60;
        }

        public static double SecondsToNano(this double value)
        {
            return value * 1000000000;
        }

        public static long SecondsToNano(this long value)
        {
            return value * 1000000000;
        }

        public static int StepToMS(int step)
        {
            return step * 3;
        }

        public static int StepToPercent(int step)
        {
            return Convert.ToInt32(Math.Round(step / 255D * 100));
        }

        // public static double GetUnixEpoch(this DateTime dateTime)
        // {
        //    var unixTime = dateTime.ToUniversalTime() -
        //        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // return unixTime.TotalSeconds;
        // }
    }
}