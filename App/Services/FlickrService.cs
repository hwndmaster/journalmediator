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

        public FlickrService(IPhotoProcessor photoProcessor, IUiController ui, FlickrConfig config)
        {
            _ui = ui;
            _config = config;
            _photoProcessor = photoProcessor;
            _flickr = new Flickr(_config.ApiKey, _config.ApiSecret);

            this.Authenticate();
        }

        public Task<Photoset> GetAlbumAsync(string albumName)
        {
            var task = new TaskCompletionSource<Photoset>();
            _flickr.PhotosetsGetListAsync((FlickrResult<PhotosetCollection> result) => {
                var albums = result.Result;
                var album = albums.FirstOrDefault(x => x.Title == albumName);
                task.SetResult(album);
            });
            return task.Task;
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

            var task = new TaskCompletionSource<PhotosetPhotoCollection>();
            _flickr.PhotosetsGetPhotosAsync(album.PhotosetId, extras, (FlickrResult<PhotosetPhotoCollection> flickrResult) => {
                task.SetResult(flickrResult.Result);
            });
            var result = await task.Task;
            var photos = result.Select(x => new Models.PhotoInfo(x)).ToList();

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

                var taskUpload = new TaskCompletionSource<string>();
                _flickr.UploadPictureAsync(stream, photo.Name, photo.Name,
                    string.Empty, null, true, false, false, ContentType.None,
                    SafetyLevel.None, HiddenFromSearch.None, (FlickrResult<string> flickrResult) => {
                        taskUpload.SetResult(flickrResult.Result);
                    });
                var photoId = await taskUpload.Task;

                album = album ?? await GetOrAddAlbumAsync(albumName, photoId);

                var taskAddPhotoToAlbum = new TaskCompletionSource<NoResponse>();
                _flickr.PhotosetsAddPhotoAsync(album.PhotosetId, photoId, (FlickrResult<NoResponse> flickResult) => {
                    _ui.WritePropertyLine(string.Empty, "Done");
                    taskAddPhotoToAlbum.SetResult(flickResult.Result);
                });
                await taskAddPhotoToAlbum.Task;
            }
        }

        private void Authenticate()
        {
            _flickr.OAuthAccessToken = _config.AuthAccessToken;
            _flickr.OAuthAccessTokenSecret = _config.AuthAccessSecret;

            return;

            var requestToken = _flickr.OAuthGetRequestToken("oob");
            var url = _flickr.OAuthCalculateAuthorizationUrl(requestToken.Token, AuthLevel.Write);

            Console.WriteLine("Open this url:");
            Console.WriteLine("\t" + url);
            Console.WriteLine("and write the given token here:");
            var verifier = Console.ReadLine();

            var accessToken = _flickr.OAuthGetAccessToken(requestToken, verifier);
            _flickr.OAuthAccessToken = accessToken.Token;
            _flickr.OAuthAccessTokenSecret = accessToken.TokenSecret;
        }

        private async Task<Photoset> GetOrAddAlbumAsync(string albumName, string primaryPhotoId)
        {
            var album = await GetAlbumAsync(albumName);
            if (album == null)
            {
                var task = new TaskCompletionSource<Photoset>();

                _flickr.PhotosetsCreateAsync(albumName, string.Empty, primaryPhotoId, (FlickrResult<Photoset> flickrResult) => {
                    task.SetResult(flickrResult.Result);
                });

                album = await task.Task;
            }
            return album;
        }
    }
}