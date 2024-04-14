using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StableDiffusionTagManager.Collections
{
    public class OrderedSetObservableCollection<T> : ObservableCollection<T>
    {
        private readonly SortedSet<T> set;

        public OrderedSetObservableCollection(Comparison<T> comparison) : base()
        {
            set = new SortedSet<T>(Comparer<T>.Create(comparison));
        }

        public new void Add(T item)
        {
            if (set.Add(item))
            {
                base.Insert(FindInsertionIndex(item), item);
            }
        }

        public new bool Remove(T item)
        {
            if (set.Remove(item))
            {
                base.Remove(item);
                return true;
            }
            return false;
        }

        public new void Clear()
        {
            set.Clear();
            base.Clear();
        }

        public new bool Contains(T item)
        {
            return set.Contains(item);
        }

        private int FindInsertionIndex(T item)
        {
            int index = 0;
            foreach (T existingItem in set)
            {
                if (set.Comparer.Compare(existingItem, item) >= 0)
                {
                    break;
                }
                index++;
            }
            return index;
        }
    }
}
