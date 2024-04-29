using System.ComponentModel;

namespace StableDiffusionTagManager.Extensions
{
    public static class StringExtensions
    {
        public static T? ConvertTo<T>(this string data)
        {
            TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
            return (T?)conv.ConvertFromInvariantString(data);
        }
    }
}
