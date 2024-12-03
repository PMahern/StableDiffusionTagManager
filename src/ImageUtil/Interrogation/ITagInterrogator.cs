namespace ImageUtil
{
    public interface ITagInterrogator<TArgs> : IDisposable
    {
        Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack);
        Task<List<string>> TagImage(TArgs args, byte[] imageData, Action<string> consoleCallBack);
    }
}