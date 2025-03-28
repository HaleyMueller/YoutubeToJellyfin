﻿using AngleSharp.Dom;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Drawing;
using System.Net;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMusic.BLL
{
    public class YoutubeExplodeClient
    {
        private YoutubeClient _client = new YoutubeClient();
        public string FolderPath { get; private set; }
        
		public YoutubeExplodeClient(string folderPath)
        {
            FolderPath = folderPath;
            _client = new YoutubeClient();
        }

        public async Task<ErrorModel> ConvertFromVideoURLAsync(string URL)
        {
            try
            {
                Console.WriteLine($"Starting download for: {URL}");

                var video = await _client.Videos.GetAsync(URL);

                var streamManifest = await _client.Videos.Streams.GetManifestAsync(URL);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                var fullPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title}.{streamInfo.Container}"));

                await _client.Videos.Streams.DownloadAsync(streamInfo, fullPath);

                Console.WriteLine($"Downloaded from video successfully: {URL}");

                var thumbnailURL = video.Thumbnails.GetWithHighestResolution().Url;
                string thumbnailPath = Path.Combine(FolderPath, $"{Guid.NewGuid().ToString()}.{thumbnailURL.Split(".").Last()}");

                var convertedAudioPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title}.ogg"));

                Console.WriteLine($"Coverting in ffmpeg: {fullPath}");
                FFMPEG.ConvertFile(fullPath, convertedAudioPath);
                Console.WriteLine($"Converstion done. Made new file: {convertedAudioPath}");

                Uri uri = new Uri(thumbnailURL);
                thumbnailPath = Path.Combine(FolderPath, MakeValidFileName(video.Id + "." + uri.AbsolutePath.Split('.').Last()));

                Console.WriteLine($"Thumbnail downloading: {thumbnailURL}");
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(thumbnailURL), thumbnailPath);
                }
                Console.WriteLine($"Thumbnail done downloading: {thumbnailPath}");

                var convertedThumbnailPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Id}.png"));

                var hasSolidBorders = HasSolidBorders(thumbnailPath);
                Console.WriteLine($"Thumbnail is {(hasSolidBorders ? "" : "not")} a square format");

                if (hasSolidBorders)
                {
                    Console.WriteLine($"Thumbnail is being clipped into a square format");
                    FFMPEG.ConvertToSquarePngByTrimmingBorders(thumbnailPath, convertedThumbnailPath);
                }
                else
                {
                    Console.WriteLine($"Thumbnail is being squished into a square format");
                    FFMPEG.ConvertToSqaurePngByShrinking(thumbnailPath, convertedThumbnailPath);
                }

                Console.WriteLine($"Tagging file: {convertedAudioPath}");
                var file = TagLib.File.Create(convertedAudioPath);
                file.Tag.Title = video.Title;
                file.Tag.Performers = new string[] { video.Author.Title };
                file.Tag.Album = "UNKNOWN";
                file.Tag.Pictures = new TagLib.Picture[] { new TagLib.Picture(convertedThumbnailPath) };
                file.Save();
                Console.WriteLine($"File tagged: {file.Tag.Title} {file.Tag.Performers?.FirstOrDefault()}");

                File.Delete(convertedThumbnailPath);
            }
            catch (Exception ex)
            {
                return new ErrorModel(ex);
            }

            return null;
        }

		static bool HasSolidBorders(string imagePath)
		{
            using (Image<Rgba32> image = Image.Load<Rgba32>(imagePath))
            {
                int width = image.Width;
                int height = image.Height;

                // Check if each border is a solid color
                bool leftTopSolid = IsSolidColor(image, 0, 0, 1, height);
                bool rightTopSolid = IsSolidColor(image, width - 1, 0, 1, height);

                if (leftTopSolid && rightTopSolid)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        static bool AreColorsSimilar(Rgba32 c1, Rgba32 c2, float threshold)
        {
            float rDiff = c1.R - c2.R;
            float gDiff = c1.G - c2.G;
            float bDiff = c1.B - c2.B;

            float distance = MathF.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

            return distance < threshold;
        }

        static bool IsSolidColor(Image<Rgba32> image, int startX, int startY, int width, int height)
        {
            Rgba32 firstPixel = image[startX, startY];
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {

                    var isSimilar = AreColorsSimilar(firstPixel, image[x, y], 10f);

                    if (isSimilar == false)
                    {
                        return false;
                    }
                }
            }
            return true; // All pixels in this region are the same
        }

        public async Task<List<ErrorModel>> ConvertFromPlaylistURLAsync(string URL)
		{
            var errors = new List<ErrorModel>();
			var videos = await _client.Playlists.GetVideosAsync(URL);

			foreach(PlaylistVideo video in videos)
			{
				var error = await ConvertFromVideoURLAsync(video.Url);

                if (error != null)
                {
                    errors.Add(error);
                }
			}

            return errors;
		}

		public async Task<List<ErrorModel>> ConvertFromTextFileAsync(string path)
		{
			string[] URLs = File.ReadAllLines(path);
            //List<string> errors = new();
            var errors = new List<ErrorModel>();

            foreach (string URL in URLs)
			{
				if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute))
				{
					errors.Add(new ErrorModel($"Incorrect URI format for: {URL}"));
					continue;
				}

				var error = await ConvertFromVideoURLAsync(URL);

                if (error != null)
                {
                    errors.Add(error);
                }
            }

			return errors;
		}

		private static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
		}
	}
}
