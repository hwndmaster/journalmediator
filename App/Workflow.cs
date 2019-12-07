using System;
using System.Linq;
using System.Threading.Tasks;
using JournalMediator.Services;
using PhotoInfo = JournalMediator.Models.PhotoInfo;

namespace JournalMediator
{
    public class Workflow
    {
        private readonly IFileService _fileSvc;
        private readonly IFlickrService _flickr;
        private readonly IInputDocumentParser _inputDocParser;
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IPostFormatter _formatter;
        private readonly IUiController _ui;
        private readonly IValidator _validator;

        public Workflow(IUiController uiController, IFlickrService flickrService,
            IInputDocumentParser inputDocumentParser, IFileService fileService,
            IValidator validator, IPostFormatter formatter, IPhotoProcessor photoProcessor)
        {
            _ui = uiController;
            _flickr = flickrService;
            _inputDocParser = inputDocumentParser;
            _fileSvc = fileService;
            _formatter = formatter;
            _validator = validator;
            _photoProcessor = photoProcessor;
        }

        public async Task Run(string fileName, int? chapterNo, bool localLinking)
        {
            try
            {
                var inputDoc = await _inputDocParser.ExtractMetadata(fileName);
                _ui.PrintInputInfo(inputDoc);
                var chapter = GetOrAskForChapter(inputDoc, chapterNo);

                var photos = _inputDocParser.GetPhotosUsedInContent(chapter.Content);
                inputDoc.PhotoFilePaths = _fileSvc.GatherPhotoPaths(inputDoc, photos).ToArray();

                var validationErrors = _validator.CheckGatheredFiles(photos, inputDoc.PhotoFilePaths).ToList();
                if (localLinking)
                {
                    validationErrors.ForEach(x => _ui.Danger(x));
                }
                else
                {
                    throw new InvalidOperationException(string.Join("\r\n", validationErrors));
                }

                var photosOnServer = Enumerable.Empty<PhotoInfo>();
                if (!localLinking)
                {
                    photosOnServer = await _flickr.GetPhotosAsync(inputDoc);
                    var photosToUpload = photos.Except(photosOnServer.Select(x => x.Title.ToLower())).ToList();

                    await _flickr.UploadPhotosAsync(inputDoc.AlbumName, inputDoc.PhotoFilePaths.Where(x => photosToUpload.Contains(x.Name)), true);
                    photosOnServer = await _flickr.GetPhotosAsync(inputDoc, true);
                }
                else
                {
                    photosOnServer = inputDoc.PhotoFilePaths.Select(_photoProcessor.CreatePhotoInfoFromFile);
                }

                var output = _formatter.FormatPost(chapter, photosOnServer, localLinking);
                await _fileSvc.SaveOutputAsync(inputDoc, chapter, output);

                _ui.WriteLine("Done!");
            }
            catch (Exception ex)
            {
                _ui.Danger(ex.Message);
                _ui.Danger("Application stopped.");
            }

            Console.ReadKey();
        }

        private Models.InputChapter GetOrAskForChapter(Models.InputDocument inputDoc, int? chapterNo)
        {
            var chapter = inputDoc.Chapters[0];
            if (chapterNo != null)
            {
                chapter = inputDoc.Chapters[chapterNo.Value - 1];
            }
            else if (inputDoc.Chapters.Length > 1)
            {
                chapter = _ui.AskUserForChapter(inputDoc);
            }
            _ui.PrintSelectedChapter(chapter);
            return chapter;
        }
    }
}