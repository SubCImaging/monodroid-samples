// <copyright file="Interval.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>The Range class.</summary>
    /// <typeparam name="T">Generic parameter.</typeparam>
    public class Interval<T> where T : IComparable<T>
    {
        /// <summary>Gets or sets minimum value of the range.</summary>
        public T Minimum { get; set; }

        /// <summary>Gets or sets maximum value of the range.</summary>
        public T Maximum { get; set; }

        /// <summary>Presents the Range in readable format.</summary>
        /// <returns>String representation of the Range.</returns>
        public override string ToString()
        {
            return string.Format("[{0} - {1}]", this.Minimum, this.Maximum);
        }

        /// <summary>Determines if the range is valid.</summary>
        /// <returns>True if range is valid, else false.</returns>
        public bool IsValid()
        {
            return this.Minimum.CompareTo(this.Maximum) <= 0;
        }

        /// <summary>Determines if the provided value is inside the range.</summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the value is inside Range, else false.</returns>
        public bool ContainsValue(T value)
        {
            return (this.Minimum.CompareTo(value) <= 0) && (value.CompareTo(this.Maximum) <= 0);
        }

        /// <summary>Determines if this Range is inside the bounds of another range.</summary>
        /// <param name="Range">The parent range to test on.</param>
        /// <returns>True if range is inclusive, else false.</returns>
        public bool IsInsideInteval(Interval<T> interval)
        {
            return this.IsValid() && interval.IsValid() && interval.ContainsValue(this.Minimum) && interval.ContainsValue(this.Maximum);
        }

        /// <summary>Determines if another range is inside the bounds of this range.</summary>
        /// <param name="Range">The child range to test.</param>
        /// <returns>True if range is inside, else false.</returns>
        public bool ContainsRange(Interval<T> interval)
        {
            return this.IsValid() && interval.IsValid() && this.ContainsValue(interval.Minimum) && this.ContainsValue(interval.Maximum);
        }

        /// <summary>Determines if another range touches the bounds of this range.</summary>
        /// <param name="Range">The child range to test.</param>
        /// <returns>True if range is inside, else false.</returns>
        public bool IsTouching(Interval<T> interval)
        {
            return this.IsValid() && interval.IsValid() && (this.ContainsValue(interval.Minimum) || this.ContainsValue(interval.Maximum));
        }

        public Interval<T> Combine(Interval<T> interval)
        {
            return new Interval<T>()
            {
                Minimum = Minimum.CompareTo(interval.Minimum) < 0 ? Minimum : interval.Minimum,
                Maximum = Maximum.CompareTo(interval.Maximum) > 0 ? Maximum : interval.Maximum,
            };
        }

        public Interval<T> Intersect(Interval<T> interval)
        {
            return new Interval<T>()
            {
                Minimum = Minimum.CompareTo(interval.Minimum) > 0 ? Minimum : interval.Minimum,
                Maximum = Maximum.CompareTo(interval.Maximum) < 0 ? Maximum : interval.Maximum,
            };
        }

        public static IList<Interval<T>> CombineIntervals(IList<Interval<T>> collection)
        {
            if (collection == null || collection.Count() < 2)
            {
                return collection;
            }

            var newList = new ObservableCollection<Interval<T>>();
            var last = collection.First();

            foreach (var next in collection.Skip(1))
            {
                if (last.IsTouching(next))
                {
                    last = new Interval<T>()
                    {
                        Maximum = next.Maximum,
                        Minimum = last.Minimum,
                    };
                }
                else
                {
                    if (last.IsValid())
                    {
                        newList.Add(last);
                    }

                    last = next;
                }
            }

            if (last.IsValid())
            {
                newList.Add(last);
            }

            return newList;
        }
    }
}
