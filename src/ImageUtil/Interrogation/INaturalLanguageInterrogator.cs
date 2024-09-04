namespace ImageUtil
{
    public interface INaturalLanguageInterrogator
    {
        Task Initialize(Action<string> updateCallBack);
        Task<string> CaptionImage(byte[] imageData);
    }
}