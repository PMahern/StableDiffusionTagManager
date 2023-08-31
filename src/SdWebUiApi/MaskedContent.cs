using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SdWebUpApi
{
    public enum MaskedContent : int
    {
        Fill = 0,
        Original = 1,
        LatentNoise = 2,
        LatentNothing = 3
    }
}
