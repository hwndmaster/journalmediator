using FlickrNet;

namespace JournalMediator.Models
{
    public class PhotoInfo
    {
        public PhotoInfo()
        {
        }

        public PhotoInfo(Photo flickrPhoto)
        {
            Title = flickrPhoto.Title;
            Width = flickrPhoto.OriginalWidth;
            Height = flickrPhoto.OriginalHeight;
            WebUrl = flickrPhoto.WebUrl;
            Small320Url = flickrPhoto.Small320Url;
            Medium640Url = flickrPhoto.Medium640Url;
            Medium800Url = flickrPhoto.Medium800Url;
            LargeUrl = flickrPhoto.LargeUrl;
        }

        public string Title { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string WebUrl { get; set; }
        public string Small320Url { get; set; }
        public string Medium640Url { get; set; }
        public string Medium800Url { get; set; }
        public string LargeUrl { get; set; }
    }
}
