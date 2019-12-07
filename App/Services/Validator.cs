using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JournalMediator.Models;

namespace JournalMediator.Services
{
    public interface IValidator
    {
        IEnumerable<string> CheckGatheredFiles(string[] photoNames, PhotoFile[] files);
    }

    public class Validator : IValidator
    {
        public IEnumerable<string> CheckGatheredFiles(string[] photoNames, PhotoFile[] files)
        {
            var fileNamesOnly = files.Select(x => x.Name).ToArray();
            if (fileNamesOnly.Distinct().Count() != files.Length)
            {
                yield return "The files sources contain photos with the same name";
            }

            var photosWithNoFiles = photoNames.Select(x => x.ToLower()).Except(fileNamesOnly);
            if (photosWithNoFiles.Any())
            {
                yield return $"The following photos have no source files:\r\n{(string.Join(", ", photosWithNoFiles))}";
            }
        }
    }
}
