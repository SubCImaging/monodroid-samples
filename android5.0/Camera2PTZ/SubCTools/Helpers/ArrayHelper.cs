//-----------------------------------------------------------------------
// <copyright file="ArrayHelper.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Scott Maher</author>
//-----------------------------------------------------------------------
namespace SubCTools.Helpers
{
    using System.Collections.Generic;

    /// <summary>
    /// A static class filled with extension and helper methods for Arrays.
    /// </summary>
    public static class ArrayHelper
    {
        /// <summary>
        /// Test to see if both arrays contain the same elements.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (ReferenceEquals(a1, a2))
            {
                return true;
            }

            if (a1 == null || a2 == null)
            {
                return false;
            }

            if (a1.Length != a2.Length)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < a1.Length; i++)
            {
                if (!comparer.Equals(a1[i], a2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
