using System;
using JournalMediator.Models;

namespace JournalMediator
{
    public interface IUiController
    {
        void PrintInputInfo(InputDocument input);
        void PrintSelectedChapter(InputChapter chapter);
        InputChapter AskUserForChapter(InputDocument input);
        void Danger(string text);
        void Write(string text);
        void WriteLine(string text);
        void WriteProperty(string caption, string value = null, string postfix = null);
        void WritePropertyLine(string caption, string value, string postfix = null);
    }

    public class UiController : IUiController
    {
        ConsoleColor _defaultColor;

        public UiController()
        {
            _defaultColor = Console.ForegroundColor;
        }

        public void PrintInputInfo(InputDocument input)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("Document information:");
            WritePropertyLine("  ° Album name:\t", input.AlbumName);
            foreach (var photoPath in input.PhotoPaths)
            {
                WritePropertyLine("  ° Photo path:\t", photoPath);
            }
            Console.WriteLine();
            Console.ForegroundColor = _defaultColor;
        }

        public void PrintSelectedChapter(InputChapter chapter)
        {
            WritePropertyLine("Chapter selected: ", chapter.Title);
        }

        public InputChapter AskUserForChapter(InputDocument input)
        {
            Console.WriteLine("Select a chapter:");
            for (var i = 0; i < input.Chapters.Length; i++)
            {
                Write(true, new [] { $"  {i+1}", $". {input.Chapters[i].Title}" },
                    ConsoleColor.Magenta, Console.ForegroundColor);
            }
            var chapterIndex = int.Parse(Console.ReadLine());
            return input.Chapters[chapterIndex - 1];
        }

        public void Danger(string text)
        {
            Write(true, new [] { text }, ConsoleColor.Red);
        }

        public void Write(string text)
        {
            Console.Write(text);
        }

        public void WriteLine(string text)
        {
            Console.WriteLine(text);
        }

        public void WriteProperty(string caption, string value = null, string postfix = null)
        {
            Write(false, new string[] { caption, value, postfix },
                Console.ForegroundColor, ConsoleColor.Green, Console.ForegroundColor);
        }

        public void WritePropertyLine(string caption, string value = null, string postfix = null)
        {
            Write(true, new string[] { caption, value, postfix },
                Console.ForegroundColor, ConsoleColor.Green, Console.ForegroundColor);
        }

        private void Write(bool addNewLine, string[] values, params ConsoleColor[] colors)
        {
            var originalColor = Console.ForegroundColor;

            for (var i = 0; i < values.Length; i++)
            {
                Console.ForegroundColor = colors[i];
                Console.Write(values[i]);
            }
            if (addNewLine)
            {
                Console.WriteLine();
            }
            Console.ForegroundColor = originalColor;
        }
    }
}