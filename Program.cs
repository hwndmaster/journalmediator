using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FlickrNet;
using JournalMediator.Models;
using JournalMediator.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JournalMediator
{
    class Program
    {
        public static IConfiguration Configuration { get; set; }

        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption();
            var optFileName = app.Option("-f <FILENAME>", "Input document file name", CommandOptionType.SingleValue);
            var optChapter = app.Option<int>("-c <CHAPTER_NUMBER>", "Chapter number", CommandOptionType.SingleValue);

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            var serviceProvider = GetServiceProvider();

            app.OnExecute(async () =>
            {
                await serviceProvider.GetService<Workflow>().Run(
                    optFileName.Value(),
                    optChapter.HasValue() ? optChapter.ParsedValue : (int?)null);
            });

            return app.Execute(args);
        }

        private static ServiceProvider GetServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton<Workflow>()
                .AddSingleton<IFileService, FileService>()
                .AddSingleton<IFlickrService, FlickrService>()
                .AddSingleton<IInputDocumentParser, InputDocumentParser>()
                .AddSingleton<IPostFormatter, PostFormatter>()
                .AddSingleton<IPhotoProcessor, PhotoProcessor>()
                .AddSingleton<IUiController, UiController>()
                .AddSingleton<IValidator, Services.Validator>()
                .AddSingleton<IConfiguration>(Program.Configuration)
                .AddSingleton<FlickrConfig>((serviceProvider) =>
                    Program.Configuration.GetSection("Flickr").Get<FlickrConfig>())
                .BuildServiceProvider();
        }
    }
}
