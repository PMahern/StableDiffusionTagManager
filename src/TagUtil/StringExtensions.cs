namespace TagUtil
{
    internal static class StringExtensions
    {
        public static List<string> ExtractTags(this string _this)
        {
            return _this.Split(',').Select(s => s.Trim()).Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }
    }
}
