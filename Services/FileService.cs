using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JournalMediator.Models;

namespace JournalMediator.Services
{
    public interface IFileService
    {
        IEnumerable<PhotoFile> GatherPhotoPaths(InputDocument inputDoc, string[] fileNames);
        Task SaveOutputAsync(InputDocument inputDoc, InputChapter chapter, string output);
    }

    public class FileService : IFileService
    {
        public IEnumerable<PhotoFile> GatherPhotoPaths(InputDocument inputDoc, string[] fileNames)
        {
            return from pp in inputDoc.PhotoPaths
                   from filePath in Directory.GetFiles(pp, "*.*", SearchOption.AllDirectories)
                   let fileName = Path.GetFileNameWithoutExtension(filePath)
                   where fileNames.Any(f => fileName.Equals(f, StringComparison.OrdinalIgnoreCase))
                   select CreatePhotoFile(fileName.ToLower(), filePath);
        }

        public async Task SaveOutputAsync(InputDocument inputDoc, InputChapter chapter, string output)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputDoc.FilePath);
            var path = Path.Combine(Path.GetDirectoryName(inputDoc.FilePath),
                $"{fileName}_chapter{chapter.ChapterNo}_output.html");
            await File.WriteAllTextAsync(path, output);
        }

        private PhotoFile CreatePhotoFile(string fileName, string filePath)
        {
            return new PhotoFile {
                Name = fileName.ToLower(),
                FilePath = filePath
            };
        }
    }
}
