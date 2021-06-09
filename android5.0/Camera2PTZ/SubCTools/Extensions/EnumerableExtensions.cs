// <copyright file="EnumerableExtensions.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    public static class EnumerableExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name != null)
            {
                var field = type.GetField(name);
                if (field != null)
                {
                    if (Attribute.GetCustomAttribute(
                               field,
                             typeof(DescriptionAttribute)) is DescriptionAttribute attr)
                    {
                        return attr.Description;
                    }
                }
            }

            return null;
        }

        public static long Nearest(this IEnumerable<long> source, long value)
        {
            return source?.Aggregate((current, next) => System.Math.Abs(current - value) < System.Math.Abs(next - value) ? current : next) ?? -1;
        }

        public static int Nearest(this IEnumerable<int> source, int value)
        {
            if (source.Count() == 0)
            {
                return default(int);
            }

            return source?.Aggregate((current, next) => System.Math.Abs(current - value) < System.Math.Abs(next - value) ? current : next) ?? -1;
        }

        // public static int Nearest(this IEnumerable<int> source, int value) => Nearest((long)value);
    }
}