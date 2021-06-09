//-----------------------------------------------------------------------
// <copyright file="RectExtensions.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------
namespace SubCTools.Droid.Extensions
{
    using Android.App;
    using Android.Content;
    using Android.Graphics;
    using Android.OS;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class RectExtensions
    {
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

        public static double AspectRatio(this Rect rect)
        {
            return rect.Width() / rect.Height();
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