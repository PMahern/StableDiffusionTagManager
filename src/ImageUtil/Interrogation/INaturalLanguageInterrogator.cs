namespace ImageUtil
{
    public interface INaturalLanguageInterrogator<TArgs> : IDisposable
    {
        Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack);
        Task<string> CaptionImage(TArgs args, byte[] imageData, Action<string> consoleCallBack);
    }
}