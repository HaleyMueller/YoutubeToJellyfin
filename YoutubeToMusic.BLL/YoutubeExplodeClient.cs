using AngleSharp.Dom;
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
        private string folderPath = @"C:\Users\Haley\Desktop\test";
        

		public YoutubeExplodeClient()
        {
            _client = new YoutubeClient();
        }

        public async Task ConvertFromVideoURLAsync(string URL)
        {
			Console.WriteLine($"Starting download for: {URL}");

			var video = await _client.Videos.GetAsync(URL);

            var streamManifest = await _client.Videos.Streams.GetManifestAsync(URL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            var fullPath = Path.Combine(folderPath, MakeValidFileName($"{video.Title}.{streamInfo.Container}"));

			await _client.Videos.Streams.DownloadAsync(streamInfo, fullPath);

            Console.WriteLine($"Downloaded from video successfully: {URL}");

            var thumbnailURL = video.Thumbnails.GetWithHighestResolution().Url;
			string thumbnailPath = Path.Combine(folderPath, $"{Guid.NewGuid().ToString()}.{thumbnailURL.Split(".").Last()}");

			var convertedAudioPath = Path.Combine(folderPath, MakeValidFileName($"{video.Title}.ogg"));

            Console.WriteLine($"Coverting in ffmpeg: {fullPath}");
            FFMPEG.ConvertFile(fullPath, convertedAudioPath);
            Console.WriteLine($"Converstion done. Made new file: {convertedAudioPath}");

			Uri uri = new Uri(thumbnailURL);
            thumbnailPath = Path.Combine(folderPath, MakeValidFileName(video.Id+"."+uri.AbsolutePath.Split('.').Last()));

            Console.WriteLine($"Thumbnail downloading: {thumbnailURL}");
            using (WebClient client = new WebClient())
			{
				client.DownloadFile(new Uri(thumbnailURL), thumbnailPath);
			}
            Console.WriteLine($"Thumbnail done downloading: {thumbnailPath}");

			var convertedThumbnailPath = Path.Combine(folderPath, MakeValidFileName($"{video.Id}.png"));

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
        }

		static bool HasSolidBorders(string imagePath)
		{
            using (Image<Rgba32> image = Image.Load<Rgba32>(imagePath))
            {
                int width = image.Width;
                int height = image.Height;

                // Check if each border is a solid color
                bool leftSolid = IsSolidColor(image, 0, 0, 1, height);
                bool rightSolid = IsSolidColor(image, width - 1, 0, 1, height);

                if (leftSolid && rightSolid)
                {
                    Console.WriteLine("The image has a solid border."); 
                    return true;
                }
                else
                {
                    Console.WriteLine("The image does not have a solid border."); 
                    return false;
                }
            }
        }

        static bool IsSolidColor(Image<Rgba32> image, int startX, int startY, int width, int height)
        {
            Rgba32 firstPixel = image[startX, startY];
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    if (!image[x, y].Equals(firstPixel))
                    {
                        return false; // Found a different color, not a solid border
                    }
                }
            }
            return true; // All pixels in this region are the same
        }

        public async Task ConvertFromPlaylistURLAsync(string URL)
		{
			var videos = await _client.Playlists.GetVideosAsync(URL);

			foreach(PlaylistVideo video in videos)
			{
				await ConvertFromVideoURLAsync(video.Url);
			}
			
		}

		public async Task<List<string>> ConvertFromTextFileAsync(string path)
		{
			string[] URLs = File.ReadAllLines(path);
			List<string> errors = new();

			foreach (string URL in URLs)
			{
				if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute))
				{
					errors.Add(URL);
					continue;
				}
				await ConvertFromVideoURLAsync(URL);
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
