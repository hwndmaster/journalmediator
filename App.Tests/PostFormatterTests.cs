using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using JournalMediator.Models;
using JournalMediator.Services;

namespace JournalMediator.Tests
{
    public class PostFormatterTests
    {
        private readonly Mock<IHtmlPartProvider> _htmlMock = new Mock<IHtmlPartProvider>();
        private readonly PostFormatter _formatter;

        public PostFormatterTests()
        {
            _formatter = new PostFormatter(_htmlMock.Object);

            _htmlMock.SetupGet(x => x.DivForTextStart).Returns("");
            _htmlMock.SetupGet(x => x.DivEnd).Returns("");
            _htmlMock.Setup(x => x.Centered(It.IsAny<string>())).Returns((string text) => $"c[{text}]");
            _htmlMock.Setup(x => x.Image(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((string url, string src, int height, int width) => $"<{url}/{src}/{height}/{width}>");
            _htmlMock.Setup(x => x.Link(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string url, string title) => $"<{url}-{title}>");
            _htmlMock.Setup(x => x.LjCut(It.IsAny<string>()))
                .Returns((string text) => text);
        }

        [Fact]
        public void Formatting_of_plain_text()
        {
            // Arrange
            var chapter = new InputChapter {
                Content = "test"
            };

            // Act
            var output = _formatter.FormatPost(chapter, new List<PhotoInfo>());

            // Verify
            Assert.Equal("test", output);
        }

        [Fact]
        public void Formatting_links()
        {
            // Arrange
            var chapter = new InputChapter {
                Content = "test |url|title| test"
            };

            // Act
            var output = _formatter.FormatPost(chapter, new List<PhotoInfo>());

            // Verify
            Assert.Equal("test <http://url-title> test", output);
        }

        [Fact]
        public void Formatting_single_photo()
        {
            // Arrange
            var chapter = new InputChapter {
                Content = "test\n[photo1]\n\n\ntest"
            };
            var photos = new List<PhotoInfo> {
                new PhotoInfo {
                    Title = "photo1",
                    Width = 100,
                    Height = 120,
                    WebUrl = "web-url",
                    Small320Url = "small-url"
                }
            };

            // Act
            var output = _formatter.FormatPost(chapter, photos);

            // Verify
            Assert.Equal("test\nc[<web-url/small-url/120/100>]\ntest", output);
        }
    }
}