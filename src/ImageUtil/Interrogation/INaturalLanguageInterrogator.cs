namespace ImageUtil
{
    public interface INaturalLanguageInterrogator : IDisposable
    {
        Task Initialize(Action<string> updateCallBack);
        Task<string> CaptionImage(byte[] imageData);
    }
}