using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JournalMediator.Models;

namespace JournalMediator.Services
{
    public interface IPostFormatter
    {
        string FormatPost(InputChapter chapter, IEnumerable<PhotoInfo> photos);
    }

    public class PostFormatter : IPostFormatter
    {
        private const int MAX_WIDTH = 712;
        private const int MAX_HEIGHT = 600;
        private const int GAP_BETWEEN_PHOTOS = 4;

        private readonly IHtmlPartProvider _html;

        /// <summary>
        /// Like: [DSC_9992] [DSC_9997]
        /// Or: [DSC_9992] [DSC_9997] (photo title)
        /// </summary>
        private readonly Regex _reLineWithPhotoDefinitions = new Regex(@"^\t?\[.*(\]|\))\s\s?", RegexOptions.Multiline);
        private readonly Regex _rePhotoDefinition = new Regex(@"\[(?<name>[^]]+)](\s\((?<title>[^\)]+)\))?");

        /// <summary>
        /// Like: |http://something.com/blabla/page|Some page title|
        /// </summary>
        private readonly Regex _reHyperlinkDefinition = new Regex(@"\|(?<link>.+?)\|(?<title>.+?)\|");

        public PostFormatter(IHtmlPartProvider html)
        {
            _html = html;
        }

        public string FormatPost(InputChapter chapter, IEnumerable<PhotoInfo> photos)
        {
            var content = chapter.Content;
            content = FormatPhotoPlaceholders(content, photos);
            content = FormatHyperlinks(content);
            content = FormatStrikeOuts(content);
            content = FormatBlockquotes(content);
            content = WrapContentWithBetterFont(content);
            content = InjectLjCutToPost(content);

            return content;
        }

        private string FormatHyperlinks(string content)
        {
            content = _reHyperlinkDefinition.Replace(content, (m) =>
            {
                var url = m.Groups["link"].Value;
                if (!url.Contains("://"))
                {
                    url = "http://" + url;
                }
                return _html.Link(url, m.Groups["title"].Value);
            });
            return content;
        }

        private string FormatPhotoPlaceholders(string content, IEnumerable<PhotoInfo> photos)
        {
            var photosBySourceName = photos.ToDictionary(x => x.Title);

            content = _reLineWithPhotoDefinitions.Replace(content, (m) =>
            {
                var photosInLine = _rePhotoDefinition.Matches(m.Value).Cast<Match>()
                    .Select(x => new
                    {
                        photo = photosBySourceName[x.Groups["name"].Value.ToLower()],
                        title = x.Groups["title"].Success ? x.Groups["title"].Value : null
                    }).ToArray();

                double desiredHeight = CalculateHeightForPhotosOnTheSameLine(photosInLine.Select(x => x.photo).ToList());

                var line = "";
                foreach (var photo in photosInLine)
                {
                    var desiredWidth = (int)Math.Round(photo.photo.Width * desiredHeight / photo.photo.Height);

                    if (line.Length > 0)
                        line += " ";
                    line += _html.Image(photo.photo.WebUrl,
                        GetPhotoUrlWithSize(photo.photo, desiredWidth, (int)desiredHeight),
                        (int)desiredHeight, desiredWidth);

                    if (!string.IsNullOrEmpty(photo.title))
                    {
                        line += Environment.NewLine + _html.Title(photo.title);
                    }
                }

                return WrapContentWithCenteredDiv(line);
            });
            return content;
        }

        private string GetPhotoUrlWithSize(PhotoInfo photo, int desiredWidth, int desiredHeight)
        {
            var maxSide = Math.Max(desiredWidth, desiredHeight);

            if (maxSide <= 320)
            {
                return photo.Small320Url;
            }
            if (maxSide <= 640)
            {
                return photo.Medium640Url;
            }
            if (maxSide <= 800)
            {
                return photo.Medium800Url;
            }
            return photo.LargeUrl; // 1024
        }

        /// <summary>
        /// Sample 1:
        /// blablabla -muted- blablabla
        /// Sample 2:
        /// blablabla -muted multiple words- blablabla
        /// </summary>
        private string FormatStrikeOuts(string content)
        {
            return Regex.Replace(content, @"(?<=\s)-(?<text>\w[\w\s]*\w)-(?=\s)", (m) =>
                _html.Strikeout(m.Groups["text"].Value));
        }

        /// <summary>
        /// Sample:
        ///     blablabla
        ///     {
        ///        text
        ///     }
        ///     blablabla
        /// </summary>
        private string FormatBlockquotes(string content)
        {
            return Regex.Replace(content, @"\r\n{\r\n(?<text>.+?)\r\n}\r\n", (m) =>
                _html.Blockquote(m.Groups["text"].Value),
                RegexOptions.Singleline | RegexOptions.Multiline);
        }

        private string InjectLjCutToPost(string content)
        {
            var matches = Regex.Matches(content, _html.DivEnd + @"\s{0,}");
            if (matches.Count == 0 || matches[0].Index + matches[0].Length == content.Length)
                return content;

            var splitIndex1 = matches[0].Index + matches[0].Length;
            var splitIndex2 = matches[matches.Count - 1].Index;

            content = content.Substring(0, splitIndex1)
                + _html.DivEnd + _html.LjCut(
                    _html.DivForTextStart
                    + content.Substring(splitIndex1, splitIndex2 - splitIndex1)
                    + content.Substring(splitIndex2)
                );

            return content;
        }

        private double CalculateHeightForPhotosOnTheSameLine(IReadOnlyCollection<PhotoInfo> photosInLine)
        {
            var totalGapsWidth = (photosInLine.Count - 1) * GAP_BETWEEN_PHOTOS;
            var totalWithOfPhotos = photosInLine.Sum(x => x.Width);
            double maxWidthInLine = Math.Min(MAX_WIDTH, totalWithOfPhotos) - totalGapsWidth;
            var numerator = photosInLine.Aggregate(1L, (i, photo) => i * photo.Height);
            var denominator = photosInLine.Sum(x => x.Width * photosInLine.Where(p0 => p0 != x).Aggregate(1L, (i, p0) => i * p0.Height));
            var desiredHeight = Math.Round(maxWidthInLine * numerator / denominator);
            return Math.Min(MAX_HEIGHT, desiredHeight);
        }

        private string WrapContentWithCenteredDiv(string content) => _html.Centered(content);

        private string WrapContentWithBetterFont(string content)
            => $"{_html.DivForTextStart}{content}{_html.DivEnd}";
    }
}
