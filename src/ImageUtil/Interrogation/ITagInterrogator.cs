namespace ImageUtil
{
    public interface ITagInterrogator : IDisposable
    {
        Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack);
        Task<List<string>> TagImage(byte[] imageData, float threshold);
    }
}