using Avalonia.Media.Imaging;
using Avalonia;
using ImageUtil;
using ImageUtil.Segmentation;
using StableDiffusionTagManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Attributes;

namespace StableDiffusionTagManager.Services
{
    [Service]
    public class ComicPanelExtractor
    {
        private readonly Settings settings;

        public ComicPanelExtractor(Settings settings)
        {
            this.settings = settings;
        }

        public async Task<List<Bitmap>?> ExtractComicPanels(Bitmap bitmap, RenderTargetBitmap? paint = null)
        {
            string tmpImage = Path.Combine(App.GetTempFileDirectory(), $"{Guid.NewGuid()}.png");
            bitmap.Save(tmpImage);

            var appDir = App.GetAppDirectory();

            KumikoWrapper kwrapper = new KumikoWrapper(settings.PythonPath, Path.Combine(new string[] { appDir, "Assets", "kumiko" }));
            var results = await kwrapper.GetImagePanels(tmpImage, App.GetTempFileDirectory());

            return results.Select(r => bitmap.CreateNewImageFromRegion(new Rect(r.TopLeftX, r.TopLeftY, r.Width, r.Height), null, paint)).ToList();
        }

        public async Task<List<Bitmap>?> ExtractSegmentsViaLlm(Bitmap bitmap, LlmSegmentationArgs args)
        {
            string tmpImage = Path.Combine(App.GetTempFileDirectory(), $"{Guid.NewGuid()}.png");
            bitmap.Save(tmpImage);

            var segmentor = new LlmImageSegmentor();
            var results = await segmentor.GetImageSegments(tmpImage, args, bitmap.PixelSize.Width, bitmap.PixelSize.Height);

            return results.Select(r => bitmap.CreateNewImageFromPolygon(r.Points)).ToList();
        }
    }
}
