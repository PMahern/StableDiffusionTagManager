namespace ImageUtil
{
    public interface ITagInterrogator
    {
        Task Initialize(Action<string> updateCallBack);
        Task<List<string>> TagImage(byte[] imageData, float threshold);
    }
}