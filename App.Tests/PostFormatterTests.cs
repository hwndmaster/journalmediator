using System;
using System.Collections.Generic;
using FlickrNet;
using Xunit;
using JournalMediator.Models;
using JournalMediator.Services;

namespace JournalMediator.Tests
{
    public class PostFormatterTests
    {
        private readonly PostFormatter _formatter;

        public PostFormatterTests()
        {
            _formatter = new PostFormatter();
        }

        [Fact]
        public void Formatting_of_plain_text()
        {
            // Arrange
            var chapter = new InputChapter {
                Content = "test"
            };
            var photos = new List<Photo>();

            // Act
            var output = _formatter.FormatPost(chapter, photos);

            // Verify
            Assert.Equal(_formatter.WrapContentWithBetterFont("test"), output);
        }
    }
}
