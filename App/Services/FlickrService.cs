using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlickrNet;
using JournalMediator.Models;
using Microsoft.Extensions.Configuration;
using PhotoInfo = JournalMediator.Models.PhotoInfo;

namespace JournalMediator.Services
{
    public interface IFlickrService
    {
        Task<Photoset> GetAlbumAsync(string albumName);
        Task<IEnumerable<PhotoInfo>> GetPhotosAsync(InputDocument inputDoc, bool fetchAllInfo = false);
        Task UploadPhotosAsync(string albumName, IEnumerable<PhotoFile> photos, bool resize);
    }

    public class FlickrService : IFlickrService
    {
        private readonly IPhotoProcessor _photoProcessor;
        private readonly IUiController _ui;
        private readonly Flickr _flickr;
        private readonly FlickrConfig _config;
        private FoundUser _me;

        public FlickrService(IPhotoProcessor photoProcessor, IUiController ui, FlickrConfig config)
        {
            _ui = ui;
            _config = config;
            _photoProcessor = photoProcessor;
            _flickr = new Flickr(_config.ApiKey, _config.ApiSecret);

            this.InitializeAsync().GetAwaiter().GetResult();
        }

        public async Task<Photoset> GetAlbumAsync(string albumName)
        {
            var albums = await _flickr.PhotosetsGetListAsync(this._me.UserId);
            return albums.FirstOrDefault(x => x.Title == albumName);
        }

        public async Task<IEnumerable<PhotoInfo>> GetPhotosAsync(InputDocument inputDoc, bool fetchAllInfo = false)
        {
            if (inputDoc.AlbumName == null)
            {
                return Enumerable.Empty<PhotoInfo>();
            }

            var album = await GetAlbumAsync(inputDoc.AlbumName);
            if (album == null)
            {
                return Enumerable.Empty<PhotoInfo>();
            }

            var extras = fetchAllInfo
                ? PhotoSearchExtras.OriginalDimensions | PhotoSearchExtras.Medium800Url
                    | PhotoSearchExtras.Medium640Url | PhotoSearchExtras.Small320Url
                : PhotoSearchExtras.None;
            var result = await _flickr.PhotosetsGetPhotosAsync(album.PhotosetId, extras);
            var photos = result.Select(x => new Models.PhotoInfo(x));

            if (fetchAllInfo)
            {
                foreach (var photo in photos.Where(x => x.Height == 0))
                {
                    _photoProcessor.FillUpDimensions(inputDoc, photo);
                }
            }

            return photos;
        }

        public async Task UploadPhotosAsync(string albumName, IEnumerable<PhotoFile> photos, bool resize)
        {
            Photoset album = null;

            _ui.WritePropertyLine("Uploading ", photos.Count().ToString(), " files to Flickr...");
            foreach (var photo in photos)
            {
                // TODO: Check if resize == false
                _ui.WriteProperty($"Uploading {photo.Name}...");
                _photoProcessor.ResizePhotoForUpload(photo, out Stream stream);
                var photoId = await _flickr.UploadPictureAsync(stream, photo.Name, photo.Name,
                    string.Empty, null, true, false, false, ContentType.None,
                    SafetyLevel.None, HiddenFromSearch.None);

                album = album ?? await GetOrAddAlbumAsync(albumName, photoId);

                await _flickr.PhotosetsAddPhotoAsync(album.PhotosetId, photoId);

                _ui.WritePropertyLine(string.Empty, "Done");
            }
        }

        private async Task InitializeAsync()
        {
            await Authenticate();

            this._me = await _flickr.PeopleFindByUserNameAsync(_config.Login);
        }

        private async Task Authenticate()
        {
            _flickr.OAuthAccessToken = _config.AuthAccessToken;
            _flickr.OAuthAccessTokenSecret = _config.AuthAccessSecret;

            return;

            var requestToken = await _flickr.OAuthRequestTokenAsync("http://localhost");
            var url = _flickr.OAuthCalculateAuthorizationUrl(requestToken.Token, AuthLevel.Write);

            Console.WriteLine("Open this url:");
            Console.WriteLine("\t" + url);
            Console.WriteLine("and write the given token here:");
            var verifier = Console.ReadLine();

            var accessToken = await _flickr.OAuthAccessTokenAsync(requestToken.Token, requestToken.TokenSecret, verifier);
            _flickr.OAuthAccessToken = accessToken.Token;
            _flickr.OAuthAccessTokenSecret = accessToken.TokenSecret;
        }

        private async Task<Photoset> GetOrAddAlbumAsync(string albumName, string primaryPhotoId)
        {
            var album = await GetAlbumAsync(albumName);
            if (album == null)
            {
                album = await _flickr.PhotosetsCreateAsync(albumName, string.Empty, primaryPhotoId);
            }
            return album;
        }
    }
}