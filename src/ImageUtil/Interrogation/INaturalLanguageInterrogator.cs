namespace ImageUtil
{
    public interface INaturalLanguageInterrogator : IDisposable
    {
        Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack);
        Task<string> CaptionImage(string prompt, byte[] imageData);
    }
}