using StableDiffusionTagManager.Collections;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public static List<DropdownSelectItem> ToDropdownSelectItems<T>(this IEnumerable<T> items, Func<T, string> nameSelector)
        {
            return items.Select(item => (DropdownSelectItem)new DropdownSelectItem<T>(nameSelector(item), item)).ToList();
        }
    }
}
