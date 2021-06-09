//-----------------------------------------------------------------------
// <copyright file="Enumerable.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>
// <author>Unknown</author>
//-----------------------------------------------------------------------

namespace SubCTools.Helpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A bunch of extensions for <see cref="IEnumerable{T}"/> and <see cref="IList{T}"/>.
    /// </summary>
    public static class Enumerable
    {
        /// <summary>
        /// Adds one <see cref="IEnumerable{T}"/> to another <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="o">This <see cref="IEnumerable{T}"/> to have the <see cref="Items"/> added to.</param>
        /// <param name="items">The <see cref="IEnumerable{T}"/> to add to <see cref="o"/>.</param>
        public static void Add<T>(this ICollection<T> o, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                o.Add(item);
            }
        }

        /// <summary>
        /// Adds one <see cref="IEnumerable{T}"/> to another <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="o">This <see cref="IEnumerable{T}"/> to have the <see cref="Items"/> added to.</param>
        /// <param name="items">The <see cref="IEnumerable{T}"/> to add to <see cref="o"/>.</param>
        /// <param name="condition">A conditional <see cref="Func{bool}"/>, it will not
        /// copy if it returns <see cref="false"/>.</param>
        public static void Add<T>(this ICollection<T> o, IEnumerable<T> items, Func<bool> condition)
        {
            foreach (var item in items)
            {
                if (condition())
                {
                    o.Add(item);
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="IEnumerable{T}"/> to a <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="o">This <see cref="IList{T}"/> to add the <see cref="IEnumerable{T}"/> to.</param>
        /// <param name="items">The <see cref="IEnumerable{T}"/> to add to <see cref="o"/>.</param>
        public static void AddRange<T>(this IList<T> o, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                o.Add(item);
            }
        }

        /// <summary>
        /// Check to see whether an enumerable contains an item.
        /// </summary>
        /// <typeparam name="T">Type of object to check.</typeparam>
        /// <param name="array">Array of items.</param>
        /// <param name="items">Item to see if it's in the array.</param>
        /// <returns>True if the enuerable contains the item, false if it doesn't.</returns>
        public static bool Contains<T>(this IEnumerable<T> array, IEnumerable<T> items)
        {
            foreach (var arrayItem in array)
            {
                if (items.Contains(arrayItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// iterates over two enumerables and performs action on each element of both.
        /// </summary>
        public static void DoBoth<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Action<T1, T2> action)
        {
            using (var e1 = first.GetEnumerator())
            using (var e2 = second.GetEnumerator())
            {
                while (e1.MoveNext() && e2.MoveNext())
                {
                    action(e1.Current, e2.Current);
                }
            }
        }

        // THIS APPEARS TO BE RECURSIVE, vs also complains it conflicts with line 93
        // public static void MakeEqualTo(this IList o, IList items, params object[] ignore)
        // {
        //    o.MakeEqualTo(items, ignore);
        // }

        /// <summary>
        /// A generic version of DoBoth. Iterates each element of each list and performs action.
        /// </summary>
        /// <typeparam name="T"> the type of enumerators.</typeparam>
        /// <param name="all"> an emumerable of enumerables to iterate over.</param>
        /// <param name="action">the action to perform on each item in each enumerable.</param>
        public static void ForAll<T>(this IEnumerable<IEnumerable<T>> all, Action<IEnumerable<T>> action)
        {
            var enumerators = all.Select(e => e.GetEnumerator()).ToList();
            try
            {
                while (enumerators.All(e => e.MoveNext()))
                {
                    action(enumerators.Select(e => e.Current));
                }
            }
            finally
            {
                foreach (var e in enumerators)
                {
                    e.Dispose();
                }
            }
        }

        /// <summary>
        /// Makes two <see cref="IEnumerable{T}"/> equal.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="o">This <see cref="IEnumerable{T}"/> to make equal to <see cref="items"/>.</param>
        /// <param name="items">The <see cref="IEnumerable{T}"/> to make <see cref="o"/> match.</param>
        public static void MakeEqualTo<T>(this IList<T> o, IList<T> items)
        {
            foreach (var item in o.ToArray())
            {
                if (items.Contains(item))
                {
                    continue;
                }

                // Console.WriteLine("New List doesn't contain {0}, remove", item);
                o.Remove(item);
            }

            foreach (var item in items)
            {
                if (o.Contains(item))
                {
                    continue;
                }

                // Console.WriteLine("Current List doesn't contain {0}, add", item);
                o.Add(item);
            }
        }

        /// <summary>
        /// Make the caller list contain the same elements as the items enumerable.
        /// </summary>
        /// <param name="o">Caller list to modify.</param>
        /// <param name="items">Collection to make equal to.</param>
        /// <param name="ignore">Ignore these parameters if they are in either list.</param>
        public static void MakeEqualTo<T>(this IList<T> o, IEnumerable<T> items, params T[] ignore)
        {
            // remove all the elements from list 1 that are not in list 2
            o.RemoveAll(l => o.Except(items).Contains(l));

            // add all the elements that are in list 2 that are not in list 1
            o.AddRange(items.Except(o));
        }

        //// THE BELOW METHODS ARE NOT TESTED.  ORIGINAL UNIT TESTS SHOWED THEY DONT WORK.  FURTHER TESTING NEEDED TO CONFIRM.

        /// <summary>
        /// Make the caller list contain the same elements as the items enumerable.
        /// </summary>
        /// <param name="o">Caller list to modify.</param>
        /// <param name="items">Collection to make equal to.</param>
        /// <param name="ignore">Ignore these parameters if they are in either list.</param>
        public static void MakeEqualTo<T>(this IList<T> o, IEnumerable<T> items, IEqualityComparer<T> equalityComparer, params T[] ignore)
        {
            // remove all the elements from list 1 that are not in list 2
            o.RemoveAll(l => o.Except(items, equalityComparer).Contains(l, equalityComparer));

            // add all the elements that are in list 2 that are not in list 1
            o.AddRange(items.Except(o, equalityComparer));
        }

        public static void MakeIListEqualTo(this IList o, IList items)
        {
            var array = new object[o.Count];
            o.CopyTo(array, 0);

            foreach (var item in array)
            {
                if (items.Contains(item))
                {
                    continue;
                }

                // Console.WriteLine("New List doesn't contain {0}, remove", item);
                o.Remove(item);
            }

            foreach (var item in items)
            {
                if (o.Contains(item))
                {
                    continue;
                }

                // Console.WriteLine("Current List doesn't contain {0}, add", item);
                o.Add(item);
            }
        }

        /// <summary>
        /// Remove all the items from the collection that are in the items enumerable.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="o">This <see cref="IEnumerable{T}"/> to have the
        /// <see cref="Items"/> removed from.</param>
        /// <param name="items">The <see cref="IEnumerable{T}"/> to remove from <see cref="o"/>.</param>
        public static void Remove<T>(this ICollection<T> o, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (o.Contains(item))
                {
                    o.Remove(item);
                }
            }
        }

        /// <summary>
        /// Remove all elements that match a condition from an <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="list">The <see cref="IList{T}"/> to remove the elements from.</param>
        /// <param name="condition">The <see cref="Predicate{T}"/> that
        /// determines if the element will be removed.</param>
        public static void RemoveAll<T>(this IList<T> list, Predicate<T> condition)
        {
            foreach (var item in list.ToArray())
            {
                if (condition.Invoke(item))
                {
                    list.Remove(item);
                }
            }
        }

        /// <summary>
        /// Compare two unordered lists to see if they contain the same elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in list.</typeparam>
        /// <param name="list1">First list.</param>
        /// <param name="list2">Second list.</param>
        /// <returns>True if they're the same, false if they're different.</returns>
        public static bool ScrambledEquals<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (var s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }

            foreach (var s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }

            return cnt.Values.All(c => c == 0);
        }

        /// <summary>
        /// Sets all elements of this to those of <see cref="items"/> preserving order.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="o">This <see cref="IList{T}"/> to make equal to <see cref="items"/>.</param>
        /// <param name="items">The <see cref="IEnumerable{T}"/> to make <see cref="o"/> match.</param>
        public static void SetElementsTo<T>(this IList<T> o, IEnumerable<T> items)
        {
            var newItems = items.ToArray();
            var itemsCount = items.Count();
            var longest = o.Count > itemsCount ? o.Count : itemsCount;

            for (var i = 0; i < longest; i++)
            {
                if (i >= o.Count)
                {
                    o.Add(newItems[i]);
                    continue;
                }

                if (i >= itemsCount)
                {
                    o.Remove(o.Last());
                    continue;
                }

                o[i] = newItems[i];
            }
        }
    }
}