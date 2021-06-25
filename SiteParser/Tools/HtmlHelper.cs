namespace SiteParser.Tools
{
    public static class HtmlHelper
    {
        public static string NormalizeInnerText(this string text)
        {
            return text
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Trim();
        }

        public static string NormalizeHref(this string href)
        {
            return href
                .Replace("&amp", "&");
        }
        
        
    }
}