// <copyright file="ObservableContentCollection.cs" company="SubC Imaging">
// Copyright (c) SubC Imaging. All rights reserved.
// </copyright>

namespace SubCTools.Helpers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;

    public delegate void CollectionChangingEventHandler<T>(object sender, CollectionChangingEventArgs<T> e);

    public delegate void NotifyCollectionContentChangedEventHandler(object sender, PropertyChangedEventArgs e);

    internal interface INotifyCollectionContentChanged : INotifyCollectionChanged
    {
        event NotifyCollectionContentChangedEventHandler CollectionContentChanged;
    }

    public class CollectionChangingEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionChangingEventArgs{T}"/> class.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="element"></param>
        public CollectionChangingEventArgs(CollectionChangeAction action, T element)
        {
            Action = action;
            Element = element;

            Cancel = false;
        }

        public CollectionChangeAction Action { get; private set; }

        public bool Cancel { get; set; }

        public T Element { get; private set; }
    }

    [CollectionDataContract]
    public class ObservableContentCollection<T> : ObservableCollection<T>, INotifyCollectionContentChanged
            where T : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableContentCollection{T}"/> class.
        /// </summary>
        /// <param name="enumerable"></param>
        public ObservableContentCollection(IEnumerable<T> enumerable)
            : base(enumerable)
        {
            SubscribeToPropertyChanged(enumerable);
            SubscribeToCollectionChanged();

            FireContentChanged = true;
            CanClear = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableContentCollection{T}"/> class.
        /// </summary>
        public ObservableContentCollection() : this(new List<T>())
        {
        }

        public event CollectionChangingEventHandler<T> CollectionChanging = (sender, e) => { };

        /// <inheritdoc/>
        public event NotifyCollectionContentChangedEventHandler CollectionContentChanged = (sender, e) => { };

        public bool CanClear { get; set; }

        public bool FireContentChanged { get; set; }

        public void Refresh()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            if (CanClear)
            {
                base.ClearItems();
            }
        }

        /// <inheritdoc/>
        protected override void InsertItem(int index, T item)
        {
            var cancel = OnCollectionChanging(CollectionChangeAction.Add, item);

            if (!cancel)
            {
                base.InsertItem(index, item);
            }
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            var cancel = OnCollectionChanging(CollectionChangeAction.Remove, this[index]);

            if (!cancel)
            {
                base.RemoveItem(index);
            }
        }

        private bool OnCollectionChanging(CollectionChangeAction action, T item)
        {
            var eventArgs = new CollectionChangingEventArgs<T>(action, item);

            CollectionChanging(this, eventArgs);

            return eventArgs.Cancel;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (FireContentChanged)
            {
                CollectionContentChanged(sender, e);
            }
        }

        private void SubscribeToCollectionChanged()
        {
            CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    SubscribeToPropertyChanged(e.NewItems);
                }

                if (e.OldItems != null)
                {
                    UnsubscribeFromPropertyChanged(e.OldItems);
                }
            };
        }

        private void SubscribeToPropertyChanged(IEnumerable enumerable)
        {
            foreach (var item in enumerable.Cast<INotifyPropertyChanged>())
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        private void UnsubscribeFromPropertyChanged(IEnumerable enumerable)
        {
            foreach (var item in enumerable.Cast<INotifyPropertyChanged>())
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }
    }
}