using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace YoutubeToMusic.BLL
{
    public class NfoGenerator
    {
        public static void CreateMusicVideoNfo(string mediaFilePath, string outputNfoPath)
        {
            // Read metadata with TagLib
            var file = TagLib.File.Create(mediaFilePath);

            string title = file.Tag.Title ?? Path.GetFileNameWithoutExtension(mediaFilePath);
            string album = file.Tag.Album ?? "";
            string[] artists = file.Tag.Performers ?? Array.Empty<string>();
            string[] genres = file.Tag.Genres ?? Array.Empty<string>();
            int year = (int)file.Tag.Year;
            TimeSpan duration = file.Properties.Duration;

            // Create XML
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            };

            using (var writer = XmlWriter.Create(outputNfoPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("musicvideo");

                // Required fields
                writer.WriteElementString("title", title);

                writer.WriteElementString("director", file.Tag.FirstComposer);
                writer.WriteElementString("studio", file.Tag.Publisher);

                // Optional
                if (!string.IsNullOrEmpty(album))
                    writer.WriteElementString("album", album);

                if (genres.Length > 0)
                {
                    foreach (var genre in genres)
                        writer.WriteElementString("genre", genre);
                }

                if (artists.Length > 0)
                {
                    foreach (var artist in artists)
                    {
                        writer.WriteElementString("artist", artist);

                        writer.WriteStartElement("actor");
                        writer.WriteElementString("name", artist);
                        writer.WriteElementString("role", "Artist");
                        writer.WriteElementString("order", "0");
                        writer.WriteElementString("thumb", "");
                        writer.WriteEndElement(); // actor
                    }
                }

                if (year > 0)
                    writer.WriteElementString("premiered", $"{year}-01-01");

                if (year > 0)
                    writer.WriteElementString("year", year.ToString());

                // Runtime (minutes only)
                writer.WriteElementString("runtime", Math.Round(duration.TotalMinutes).ToString());

                writer.WriteEndElement(); // musicvideo
                writer.WriteEndDocument();
            }

            Console.WriteLine($"NFO created: {outputNfoPath}");
        }

        public static void CreateArtistNfo(TagLib.File file, string outputNfoPath)
        {
            string album = file.Tag.Album ?? "";
            string[] artists = file.Tag.Performers ?? Array.Empty<string>();
            string[] genres = file.Tag.Genres ?? Array.Empty<string>();
            int year = (int)file.Tag.Year;
            TimeSpan duration = file.Properties.Duration;

            // Create XML
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = Encoding.UTF8
            };

            using (var writer = XmlWriter.Create(outputNfoPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("artist");

                // Required fields
                writer.WriteElementString("name", artists.FirstOrDefault());

                // Optional
                if (!string.IsNullOrEmpty(file.Tag.MusicBrainzArtistId))
                    writer.WriteElementString("musicBrainzArtistID", file.Tag.MusicBrainzArtistId);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            Console.WriteLine($"NFO created: {outputNfoPath}");
        }
    }
}
