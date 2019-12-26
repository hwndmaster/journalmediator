using System;

namespace JournalMediator.Services
{
    public interface IHtmlPartProvider
    {
        string Blockquote(string text);
        string Centered(string text);
        string FloatRight(string text);
        string Image(string url, string src, int height, int width);
        string Link(string url, string title);
        string LjCut(string text);
        string LjLayout(string text, string title);
        string NoImage(string imageName);
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

        public string FloatRight(string text)
            => $"<div style='float: right;'>{text}</div>";

        public string Image(string url, string src, int height, int width)
            => $"<a href='{url}'><img src='{src}' height='{height}' width='{width}' /></a>";

        public string Link(string url, string title)
            => $@"<a href='{url}' target='_blank'>{title}</a>";

        public string LjCut(string text)
            => $"<lj-cut>{text}</lj-cut>";

        public string LjLayout(string text, string title)
        {
            var textFormatted = text.Replace(Environment.NewLine, "<br/>");
            return $@"<style>a {{ color: #889 }}</style>
<body style=""background: #343f4a; color: #ccc; font-family: 'trebuchet ms',helvetica,arial,sans-serif"">
<div style=""width:770px; padding: 20px; background: #101921"">
<h3 style=""color: #f93; margin: 10px 0"">{title}</h3>{textFormatted}</div></body>";
        }

        public string NoImage(string imageName)
            => "<div style='padding: 40px 20px; border: 1px solid red; display: inline-block; color: red;'>NO PICTURE FOUND: '" + imageName + "'</div>";

        public string Strikeout(string text)
            => $"<s>{text}</s>";

        public string Title(string text)
            => "<span style='color:gray'>" + text + "</span>";

        public string DivForTextStart => "<div style='font-size:110%;font-family:Arial,sans-serif'>";

        public string DivEnd => "</div>";
    }
}