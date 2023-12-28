using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SdWebUiApi
{
    public static class RembgModels
    {
        public const string U2Net = "u2net";
        public const string U2NetP = "u2netp";
        public const string U2NetHumanSeg = "u2net_human_seg";
        public const string U2NetClothSeg = "u2net_cloth_seg";
        public const string Silueta = "u2net_human_seg";
        public const string IsNetGeneralUse = "isnet-general-use";
        public const string IsNetAnime = "isnet-anime";

        public static readonly IReadOnlyCollection<string> Models = new List<string>()
        {
            U2Net,
            U2NetP,
            U2NetHumanSeg,
            U2NetClothSeg,
            Silueta,
            IsNetGeneralUse,
            IsNetAnime
        };
    }
}
