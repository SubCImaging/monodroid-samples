using Android.Graphics;
using System.Threading;

namespace Camera2PTZ
{
    public static class Extensions
    {
        public static double AspectRatio(this Rect rect)
        {
            return rect.Width() / rect.Height();
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static double Clamp(this double value, double min = 0, double max = 100)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>
        /// Validate that a value lies within the min and max.
        /// </summary>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <returns>In-range value.</returns>
        public static int Clamp(this int value, int min = 0, int max = 100)
        {
            return (int)Clamp((float)value, min, max);
        }

        /// <summary>
        /// Returns true if the object is locked.
        /// </summary>
        /// <param name="o">the object the method is called on.</param>
        /// <returns>true if locked.</returns>
        public static bool IsLocked(this object o)
        {
            if (!Monitor.TryEnter(o))
            {
                return true;
            }

            Monitor.Exit(o);
            return false;
        }

        // Restrains the rect within a container rect while attempting to retain the current size.
        public static void Restrain(this Rect rect, Rect container)
        {
            if (!container.Contains(rect))
            {
                // A container can't contain something that's bigger than it
                if (rect.Width() > container.Width())
                {
                    rect.Left = container.Left;
                    rect.Right = container.Right;
                }

                if (rect.Height() > container.Height())
                {
                    rect.Top = container.Top;
                    rect.Bottom = container.Bottom;
                }

                var dx = 0;
                var dy = 0;

                // Bounds checking
                dx = CheckLowerBound(rect.Left, container.Left) + CheckUpperBound(rect.Right, container.Right);
                dy = CheckLowerBound(rect.Top, container.Top) + CheckUpperBound(rect.Bottom, container.Bottom);

                rect.Offset(dx, dy);
            }
        }

        private static int CheckLowerBound(int value, int lowerBound)
        {
            if (value < lowerBound)
            {
                return lowerBound - value;
            }

            return 0;
        }

        private static int CheckUpperBound(int value, int upperBound)
        {
            if (value > upperBound)
            {
                return upperBound - value;
            }

            return 0;
        }
    }
}