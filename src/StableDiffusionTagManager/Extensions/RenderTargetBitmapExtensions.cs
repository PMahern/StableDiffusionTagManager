using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Linq;
using System.Reflection;

namespace StableDiffusionTagManager.Extensions
{
    public static class RenderTargetBitmapExtensions
    {
        public static Func<RenderTargetBitmap, DrawingContext> GetDrawingContextFunc = CreateFunc();

        public static Func<RenderTargetBitmap, DrawingContext>  CreateFunc()
        {
            var platformImplProperties = typeof(RenderTargetBitmap).GetProperties(BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public);
            var platformImplProperty = platformImplProperties.First(p => p.Name == "PlatformImpl");
            var platformImplType = platformImplProperty.PropertyType;

            var itemProperties = platformImplType.GetProperties(BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public);
            var itemProperty = itemProperties.First(p => p.Name == "Item");
            var itemType = itemProperty.PropertyType;

            var funcs = itemType.GetInterface("IRenderTarget")
                            .GetMethods(BindingFlags.Instance |
                            BindingFlags.NonPublic |
                            BindingFlags.Public);
            var createFunc = funcs.First(m => m.Name == "CreateDrawingContext");
            
            var platformDrawingContextType = AppDomain.CurrentDomain.GetAssemblies()
                                                                    .SelectMany(x => x.GetTypes()
                                                                    .Where(t => t.Name == "PlatformDrawingContext" ))
                                                                    .First();
            var dcConstructor = platformDrawingContextType.GetConstructors().First();

            return (rtb) =>
            {
                var platformImpl = platformImplProperty.GetValue(rtb);
                var item = itemProperty.GetValue(platformImpl);
                var platform = createFunc.Invoke(item, null);
                return (DrawingContext)dcConstructor.Invoke(new object[] { platform, true });
            };
        }

        public static DrawingContext GetDrawingContextWithoutClear(this RenderTargetBitmap renderTargetBitmap)
        {
            return GetDrawingContextFunc(renderTargetBitmap);
        }
    }
}
