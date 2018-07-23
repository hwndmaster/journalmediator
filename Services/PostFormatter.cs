using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FlickrNet;
using JournalMediator.Models;

namespace JournalMediator.Services
{
    public interface IPostFormatter
    {
        string FormatPost(InputChapter chapter, IEnumerable<Photo> photos);
    }

    public class PostFormatter : IPostFormatter
    {
        private readonly int _maxWidth = 712;
        private readonly int _maxHeight = 600;
        private readonly int _gapBetweenPhotos = 4;

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

        public string FormatPost(InputChapter chapter, IEnumerable<Photo> photos)
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
                return $@"<a href='{url}' target='_blank'>{m.Groups["title"].Value}</a>";
            });
            return content;
        }

        private string FormatPhotoPlaceholders(string content, IEnumerable<Photo> photos)
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
                    var desiredWidth = (int)Math.Round(photo.photo.OriginalWidth * desiredHeight / photo.photo.OriginalHeight);

                    if (line.Length > 0)
                        line += " ";
                    line += $"<a href='{photo.photo.WebUrl}'><img src='{GetPhotoUrlWithSize(photo.photo, desiredWidth, (int)desiredHeight)}' height='{desiredHeight}' width='{desiredWidth}' /></a>";

                    if (!string.IsNullOrEmpty(photo.title))
                    {
                        line += Environment.NewLine + "<span style='color:gray'>" + photo.title + "</span>";
                    }
                }

                return WrapContentWithCenteredDiv(line);
            });
            return content;
        }

        private string GetPhotoUrlWithSize(Photo photo, int desiredWidth, int desiredHeight)
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
                $"<s>{m.Groups["text"].Value}</s>");
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
                $"<blockquote style='font-style: italic; color: dimgray'>{m.Groups["text"].Value}</blockquote>",
                RegexOptions.Singleline | RegexOptions.Multiline);
        }

        private string InjectLjCutToPost(string content)
        {
            var matches = Regex.Matches(content, @"</div>\s{0,}");
            if (matches.Count == 0)
                return content;

            var splitIndex1 = matches[0].Index + matches[0].Length;
            var splitIndex2 = matches[matches.Count - 1].Index;

            content = content.Substring(0, splitIndex1)
                      + "</div><lj-cut><div style='font-size:110%;font-family:Arial,sans-serif'>"
                      + content.Substring(splitIndex1, splitIndex2 - splitIndex1)
                      + content.Substring(splitIndex2)
                      + "</lj-cut>";

            return content;
        }

        private double CalculateHeightForPhotosOnTheSameLine(IReadOnlyCollection<Photo> photosInLine)
        {
            var totalGapsWidth = (photosInLine.Count - 1) * _gapBetweenPhotos;
            var totalWithOfPhotos = photosInLine.Sum(x => x.OriginalWidth);
            double maxWidthInLine = Math.Min(_maxWidth, totalWithOfPhotos) - totalGapsWidth;
            var numerator = photosInLine.Aggregate(1L, (i, photo) => i * photo.OriginalHeight);
            var denominator = photosInLine.Sum(x => x.OriginalWidth * photosInLine.Where(p0 => p0 != x).Aggregate(1L, (i, p0) => i * p0.OriginalHeight));
            var desiredHeight = Math.Round(maxWidthInLine * numerator / denominator);
            return Math.Min(_maxHeight, desiredHeight);
        }

        private string WrapContentWithCenteredDiv(string content)
        {
            return $"<div style='text-align: center'>{content}</div>";
        }

        private string WrapContentWithBetterFont(string content)
        {
            return $"<div style='font-size:110%;font-family:Arial,sans-serif'>{content}</div>";
        }
    }
}