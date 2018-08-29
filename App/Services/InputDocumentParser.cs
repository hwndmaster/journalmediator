using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JournalMediator.Models;

namespace JournalMediator.Services
{
    public interface IInputDocumentParser
    {
        Task<InputDocument> ExtractMetadata(string filePath);
        string[] GetPhotosUsedInContent(string content);
    }

    public class InputDocumentParser : IInputDocumentParser
    {
        private readonly Regex _reTitle = new Regex(@"^\/\/\/ (?<title>[\w\s]+)\r", RegexOptions.Multiline);
        private readonly Regex _rePhotoPath = new Regex(@"^\/\/\/ (?<path>[C-Z]:\\.+)\r", RegexOptions.Multiline);
        private readonly Regex _rePhotoPlaceholders = new Regex(@"\[(?<name>[^]]+)]");

        public async Task<InputDocument> ExtractMetadata(string filePath)
        {
            var result = new InputDocument {
                FilePath = filePath
            };
            var content = await File.ReadAllTextAsync(filePath);
            var titleMatch = _reTitle.Match(content);
            if (!titleMatch.Success)
            {
                throw new InvalidOperationException("Album Title cannot be determined. Check that the document should contain a line like that:\r\n/// MyAlbumName");
            }
            result.AlbumName = titleMatch.Groups["title"].Value.TrimEnd('\r', '\n', ' ');
            content = _reTitle.Replace(content, string.Empty);

            var photoPathMatches = _rePhotoPath.Matches(content);
            if (photoPathMatches.Count == 0)
            {
                throw new InvalidOperationException(@"Photos path cannot be determined. Check that the document should contain at least one line like that:\r\n/// C:\Pictures\My Trip Album");
            }
            result.PhotoPaths = photoPathMatches.Cast<Match>().Select(x => x.Groups["path"].Value).ToArray();
            content = _rePhotoPath.Replace(content, string.Empty);

            var chapterMatches = Regex.Matches(content, @"==\s(?<title>.+)\s==");
            result.Chapters = chapterMatches.Select((v, i) => ExtractChapter(chapterMatches, content, i)).ToArray();

            return result;
        }

        public string[] GetPhotosUsedInContent(string content)
        {
            return _rePhotoPlaceholders.Matches(content).Cast<Match>()
                .Select(x => x.Groups["name"].Value.ToLower()).ToArray();
        }

        private InputChapter ExtractChapter(IList<Match> chapterMatches, string content, int index)
        {
            var chapterMatch = chapterMatches[index];
            var title = chapterMatch.Groups["title"].Value;
            var contentStart = chapterMatch.Index + chapterMatch.Length + Environment.NewLine.Length;
            var contentEnd = index == chapterMatches.Count - 1
                ? content.Length
                : chapterMatches[index + 1].Index - Environment.NewLine.Length;

            return new InputChapter {
                ChapterNo = index + 1,
                Title = title,
                Content = content.Substring(contentStart, contentEnd - contentStart).Trim('\t', '\r', '\n', '/')
            };
        }
    }
}