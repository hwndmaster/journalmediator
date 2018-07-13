namespace JournalMediator.Models
{
    public class InputDocument
    {
        public string FilePath { get; set; }
        public string AlbumName { get; set; }
        public InputChapter[] Chapters { get; set; }
        public string[] PhotoPaths { get; set; }
        public PhotoFile[] PhotoFilePaths { get; set; }
    }
}
