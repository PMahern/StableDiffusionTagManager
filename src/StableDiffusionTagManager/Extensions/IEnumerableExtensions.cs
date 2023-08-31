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
    }
}
