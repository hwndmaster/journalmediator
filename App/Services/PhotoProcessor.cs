using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using JournalMediator.Models;
using Size = System.Drawing.Size;

namespace JournalMediator.Services
{
    public interface IPhotoProcessor
    {
        PhotoInfo CreatePhotoInfoFromFile(PhotoFile photoFile);
        void FillUpDimensions(InputDocument inputDoc, PhotoInfo photo);
        void ResizePhotoForUpload(PhotoFile photo, out Stream resizedPhotoStream);
    }

    public class PhotoProcessor : IPhotoProcessor
    {
        private const int MAX_SIZE = 2000;
        private const long JPEG_QUALITY = 92L;

        private readonly ImageCodecInfo _jpegEncoder;

        public PhotoProcessor()
        {
            _jpegEncoder = ImageCodecInfo.GetImageDecoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
        }

        public PhotoInfo CreatePhotoInfoFromFile(PhotoFile photoFile)
        {
            var uri = new System.Uri(photoFile.FilePath);

            var info = new Models.PhotoInfo {
                Title = photoFile.Name,
                WebUrl = uri.AbsoluteUri,
                Small320Url = uri.AbsoluteUri,
                Medium640Url = uri.AbsoluteUri,
                Medium800Url = uri.AbsoluteUri,
                LargeUrl = uri.AbsoluteUri
            };

            FillUpDimensions(photoFile, info);

            return info;
        }

        public void FillUpDimensions(InputDocument inputDoc, PhotoInfo photo)
        {
            var photoFile = inputDoc.PhotoFilePaths.First(x => x.Name == photo.Title);
            FillUpDimensions(photoFile, photo);
        }

        private void FillUpDimensions(PhotoFile photoFile, PhotoInfo photoInfo)
        {
            using (var bitmap = Image.FromFile(photoFile.FilePath))
            {
                photoInfo.Height = bitmap.Height;
                photoInfo.Width = bitmap.Width;
            }
        }

        public void ResizePhotoForUpload(PhotoFile photo, out Stream resizedPhotoStream)
        {
            using (var bitmap = Image.FromFile(photo.FilePath))
            {
                if (bitmap.Width <= MAX_SIZE && bitmap.Height <= MAX_SIZE)
                {
                    resizedPhotoStream = File.OpenRead(photo.FilePath);
                    return;
                }

                var size = CalculateBestPhotoSize(bitmap);
                var resizedPhoto = GetThumbnailImage(bitmap, size);

                var encoderParameters = new EncoderParameters(1)
                {
                    Param = {
                        [0] = new EncoderParameter(Encoder.Quality, JPEG_QUALITY)
                    }
                };

                resizedPhotoStream = new MemoryStream();
                resizedPhoto.Save(resizedPhotoStream, _jpegEncoder, encoderParameters);
                resizedPhotoStream.Seek(0, SeekOrigin.Begin);
            }
        }

        private Size CalculateBestPhotoSize(Image photo)
        {
            int width = MAX_SIZE;
            int height = MAX_SIZE;
            if (photo.Width > photo.Height)
            {
                height = (int)(MAX_SIZE * ((double)photo.Height / photo.Width));
            }
            else
            {
                width = (int)(MAX_SIZE * ((double)photo.Width / photo.Height));
            }

            return new Size(width, height);
        }

        private Image GetThumbnailImage(Image originalImage, Size thumbSize)
        {
            var target = new Bitmap(thumbSize.Width, thumbSize.Height);
            var g = Graphics.FromImage(target);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.High;

            var rect = new Rectangle(0, 0, thumbSize.Width, thumbSize.Height);
            g.DrawImage(originalImage, rect, 0, 0, originalImage.Width, originalImage.Height, GraphicsUnit.Pixel);
            g.Dispose();

            return target;
        }
    }
}