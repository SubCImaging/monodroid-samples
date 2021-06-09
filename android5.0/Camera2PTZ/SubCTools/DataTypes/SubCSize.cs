//-----------------------------------------------------------------------
// <copyright file="SubCSize.cs" company="SubC Imaging Ltd">
//     Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Mark Mercer</author>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;

namespace SubCTools.DataTypes
{
    public struct SubCSize
    {
        public static readonly SubCSize Empty;

        [JsonConstructor]
        public SubCSize(Dictionary<string, string> info)
            : this(int.Parse(info[nameof(Width)]), int.Parse(info[nameof(Height)]))
        {
        }

        public SubCSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Height { get; set; }
        public bool IsEmpty => Width == 0 || Height == 0;

        public int Width { get; set; }

        public static SubCSize Add(SubCSize sz1, SubCSize sz2) => new SubCSize(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

        public static SubCSize operator -(SubCSize sz1, SubCSize sz2) => new SubCSize(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

        public static bool operator !=(SubCSize sz1, SubCSize sz2) => !sz1.Equals(sz2);

        public static SubCSize operator +(SubCSize sz1, SubCSize sz2) => new SubCSize(sz1.Width + sz2.Width, sz1.Height + sz2.Height);

        public static bool operator ==(SubCSize sz1, SubCSize sz2) => sz1.Equals(sz2);

        public static SubCSize Subtract(SubCSize sz1, SubCSize sz2) => new SubCSize(sz1.Width - sz2.Width, sz1.Height - sz2.Height);

        public override bool Equals(object obj)
        {
            if (!(obj is SubCSize))
            {
                return false;
            }

            var o = (SubCSize)obj;

            return Width == o.Width && Height == o.Height;
        }

        public override int GetHashCode()
        {
            var hashCode = 672978199;
            hashCode = hashCode * -1521134295 + IsEmpty.GetHashCode();
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}