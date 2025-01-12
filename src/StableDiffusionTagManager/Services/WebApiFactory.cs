using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using SdWebUiApi.Api;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.Attributes;

namespace StableDiffusionTagManager.Services
{
    [Service]
    public class WebApiFactory
    {
        private readonly Settings settings;
        private readonly DialogHandler dialogHandler;

        public WebApiFactory(Settings settings, DialogHandler dialogHandler)
        {
            this.settings = settings;
            this.dialogHandler = dialogHandler;
        }

        public DefaultApi? GetWebApi()
        {
            if (string.IsNullOrEmpty(settings.WebUiAddress))
            {
                var messageBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandard("Web UI Address not set",
                                                 "The Web UI Address is not set, please set it in the settings dialog.",
                                                 ButtonEnum.Ok,
                                                 Icon.Warning);
                dialogHandler.ShowDialog(messageBoxStandardWindow);
                return null;
            }
            return new DefaultApi(settings.WebUiAddress);
        }
    }
}
