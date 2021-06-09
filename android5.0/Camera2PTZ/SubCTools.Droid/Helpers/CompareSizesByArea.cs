using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Java.Util;
using SubCTools.Droid.Callbacks;
using SubCTools.Droid.Enums;
using SubCTools.Droid.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubCTools.Droid.Helpers
{

    /// <summary>
    /// Compare two Sizes based on their areas
    /// </summary>
    public class CompareSizesByArea : Java.Lang.Object, Java.Util.IComparator
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
        {
            // We cast here to ensure the multiplications won't overflow
            if (lhs is Size && rhs is Size)
            {
                var right = (Size)rhs;
                var left = (Size)lhs;
                return Long.Signum(((long)left.Width * left.Height) - ((long)right.Width * right.Height));
            }
            else
            {
                return 0;
            }
        }
    }
}