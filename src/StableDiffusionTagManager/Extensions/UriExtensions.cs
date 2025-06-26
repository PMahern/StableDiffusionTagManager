using ImageUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Extensions
{
    public static class UriExtensions
    {
        public static string UnescapedAbsolutePath(this Uri uri)
        {
            return Utility.ConvertToUnixPath(Uri.UnescapeDataString(uri.AbsolutePath));
        }

    }
}
