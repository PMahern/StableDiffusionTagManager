using StableDiffusionTagManager.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StableDiffusionTagManager.Extensions
{
    public static class IEnumerableExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> col)
        {
            return new ObservableCollection<T>(col);
        }

        public static OrderedSetObservableCollection<T> ToOrderedSetObservableCollection<T>(this IEnumerable<T> col, System.Comparison<T> pred)
        {
            var toReturn = new OrderedSetObservableCollection<T>(pred);
            foreach(var item in col)
            {
                toReturn.Add(item);
            }
            return toReturn;
        }
    }
}
