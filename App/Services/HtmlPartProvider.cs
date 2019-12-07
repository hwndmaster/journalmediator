namespace JournalMediator.Services
{
    public interface IHtmlPartProvider
    {
        string Blockquote(string text);
        string Centered(string text);
        string Image(string url, string src, int height, int width);
        string Link(string url, string title);
        string LjCut(string text);
        string Strikeout(string text);
        string Title(string text);

        string DivForTextStart { get; }
        string DivEnd { get; }
    }

    public class HtmlPartProvider : IHtmlPartProvider
    {
        public string Blockquote(string text)
            => $"<blockquote style='font-style: italic; color: dimgray; text-align: right;'>{text}</blockquote>";

        public string Centered(string text)
            => $"<div style='text-align: center'>{text}</div>";

        public string Image(string url, string src, int height, int width)
            => $"<a href='{url}'><img src='{src}' height='{height}' width='{width}' /></a>";

        public string Link(string url, string title)
            => $@"<a href='{url}' target='_blank'>{title}</a>";

        public string LjCut(string text)
            => $"<lj-cut>{text}</lj-cut>";

        public string Strikeout(string text)
            => $"<s>{text}</s>";

        public string Title(string text)
            => "<span style='color:gray'>" + text + "</span>";

        public string DivForTextStart => "<div style='font-size:110%;font-family:Arial,sans-serif'>";

        public string DivEnd => "</div>";
    }
}