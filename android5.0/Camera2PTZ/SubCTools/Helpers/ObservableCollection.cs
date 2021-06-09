// <copyright file="ObservableCollection.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;

    public static class ObservableCollection
    {
        // Sorts the collection in the default way, raising collectionChanged along the way
        public static void Sort<T>(this ObservableCollection<T> observableCollection)
        {
            var sorted = observableCollection.OrderBy((o) => o).ToList();

            var index = 0;
            while (index < sorted.Count)
            {
                var t = sorted[index];
                observableCollection.RemoveAt(index);
                observableCollection.Insert(index, t);
                index++;
            }
        }

        // Sorts the Collection according to the given property, raising collectionChanged along the way
        public static void SortBy<T>(this ObservableCollection<T> observableCollection, Func<T, object> test)
        {
            var sorted = observableCollection.OrderBy(x => test(x)).ToList();

            var index = 0;
            while (index < sorted.Count)
            {
                if (!observableCollection[index].Equals(sorted[index]))
                {
                    var t = observableCollection[index];
                    observableCollection.RemoveAt(index);
                    observableCollection.Insert(sorted.IndexOf(t), t);
                }
                else
                {
                    index++;
                }
            }
        }
    }
}