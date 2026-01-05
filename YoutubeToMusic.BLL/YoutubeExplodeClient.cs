using AngleSharp.Dom;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Data.Common;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMusic.BLL
{
    public class YoutubeExplodeClient
    {
        public YoutubeClient _client = new YoutubeClient();
        private MusicBrainz _musicBrainz;
        public string FolderPath { get; private set; }
        
		public YoutubeExplodeClient(string folderPath)
        {
            FolderPath = folderPath;
            _client = new YoutubeClient();
            _musicBrainz = new MusicBrainz(FolderPath);
        }

        public async Task<ErrorModel> ConvertFromVideoQueryAsync(string query)
        {
            Console.WriteLine($"Query: {query}");

            try
            {
                var videos = new List<YoutubeExplode.Search.VideoSearchResult>();

                int count = 0;
                await foreach (var result in _client.Search.GetVideosAsync(query))
                {
                    //Console.WriteLine($"{result.Title} ({result.Url})");

                    videos.Add(result);

                    count++;
                    if (count >= 20)
                        break; // stop after 10 results
                }

                if (videos == null || videos.Count <= 0)
                {
                    Console.WriteLine($"Couldn't find video for query: {query}");
                    return new ErrorModel($"Couldn't find video for query: {query}");
                }

                var availableVideos = videos.Where(x => x.Title.ToUpper().Contains("AMV") == false && x.Title.ToUpper().Contains("FIRST TAKE") == false && x.Title.ToUpper().Contains("SHORTS") == false && x.Title.ToUpper().Contains("ASMV") == false && x.Title.ToUpper().Contains("FULL") == false && x.Title.ToUpper().Contains("LYRIC") == false && x.Duration > TimeSpan.FromSeconds(60));

                var video = await _client.Videos.GetAsync(availableVideos.FirstOrDefault().Id);
                return await GetVideo(video);

            }
            catch (Exception ex)
            {
                return new ErrorModel(ex.Message);
            }
        }
            
        public async Task<ErrorModel> GetVideo(YoutubeExplode.Videos.Video video)
        {
            try
            {
                var streamManifest = await _client.Videos.Streams.GetManifestAsync(video.Id);
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var videoStreamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestBitrate();

                var fullAudioPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}-audio.{audioStreamInfo.Container}"));
                await _client.Videos.Streams.DownloadAsync(audioStreamInfo, fullAudioPath);

                var fullVideoPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}-video.{videoStreamInfo.Container}"));
                await _client.Videos.Streams.DownloadAsync(videoStreamInfo, fullVideoPath);

                var fullPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}-muxed.{videoStreamInfo.Container}"));

                FFMPEG.CombineAudioVideo(fullVideoPath, fullAudioPath, fullPath);

                File.Delete(fullVideoPath);
                File.Delete(fullAudioPath);

                Console.WriteLine($"Tagging file: {fullPath}");
                var file = TagLib.File.Create(fullPath);
                file.Tag.Title = MakeValidFileName(video.Title).PadRight(30).Substring(0, 30);
                file.Tag.Performers = new string[] { video.Author.Title };
                file.Tag.Album = "UNKNOWN";
                file.Save();
                Console.WriteLine($"File tagged: {file.Tag.Title} {file.Tag.Performers?.FirstOrDefault()}");
            }
            catch (Exception ex)
            {
                return new ErrorModel(video.Title, ex);
            }

            return null;
        }

        public async Task<DataEntities.DataResponse<string>> DownloadVideo(YoutubeExplode.Videos.Video video)
        {
            var ret = new DataEntities.DataResponse<string>();

            try
            {
                var streamManifest = await _client.Videos.Streams.GetManifestAsync(video.Id);
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                var fullPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}.{streamInfo.Container}"));

                await _client.Videos.Streams.DownloadAsync(streamInfo, fullPath);

                Console.WriteLine($"Downloaded from video successfully: {video.Title}");

                var thumbnailURL = video.Thumbnails.GetWithHighestResolution().Url;

                var convertedAudioPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}.ogg"));

                Console.WriteLine($"Coverting in ffmpeg: {fullPath}");
                await FFMPEG.ConvertFile(fullPath, convertedAudioPath);
                Console.WriteLine($"Converstion done. Made new file: {convertedAudioPath}");

                ret.Data = convertedAudioPath;
            }
            catch (Exception ex)
            {
                ret.AddException(ex);
                return ret;
            }

            return ret;
        }

        public async Task<ErrorModel> ConvertVideo(YoutubeExplode.Videos.Video video)
        {
            try
            {
                var streamManifest = await _client.Videos.Streams.GetManifestAsync(video.Id);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            var fullPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}.{streamInfo.Container}"));

            await _client.Videos.Streams.DownloadAsync(streamInfo, fullPath);

            Console.WriteLine($"Downloaded from video successfully: {video.Title}");

            var thumbnailURL = video.Thumbnails.GetWithHighestResolution().Url;
            string thumbnailPath = Path.Combine(FolderPath, $"{Guid.NewGuid().ToString()}.{thumbnailURL.Split(".").Last()}");

            var convertedAudioPath = Path.Combine(FolderPath, MakeValidFileName($"{video.Title.Replace(".", "")}.ogg"));

            Console.WriteLine($"Coverting in ffmpeg: {fullPath}");
            await FFMPEG.ConvertFile(fullPath, convertedAudioPath);
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


        public async Task<ErrorModel> ConvertFromVideoURLAsync(string URL)
        {
            try
            {
                Console.WriteLine($"Starting download for: {URL}");

                var video = await _client.Videos.GetAsync(URL);

                return await ConvertVideo(video);
            }
            catch (Exception ex)
            {
                return new ErrorModel(ex);
            }

            return null;
        }

        public static async Task<string> DownloadThumbnail(string url, string id, string FolderPath, string fileType, bool doProcessing)
        {
            var convertedThumbnailPath = ""; var thumbnailPath = "";
            try
            {
                Uri uri = new Uri(url);
                thumbnailPath = Path.Combine(FolderPath, YoutubeExplodeClient.MakeValidFileName(id + "." + fileType));

                Console.WriteLine($"Thumbnail downloading: {url}");

                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var contentType = response.Content.Headers.ContentType?.MediaType;

                        var extension = contentType switch
                        {
                            "image/jpeg" => "jpg",
                            "image/png" => "png",
                            "image/gif" => "gif",
                            "image/webp" => "webp"
                        };

                        thumbnailPath = Path.Combine(FolderPath, YoutubeExplodeClient.MakeValidFileName(id + "." + extension));

                        using (var s = await client.GetStreamAsync(url))
                        {
                            using (var fs = new FileStream(thumbnailPath, FileMode.OpenOrCreate))
                            {
                                await s.CopyToAsync(fs);
                            }
                        }

                        //if (extension != "png")
                        //{
                        //    using (var img = Image.Load(thumbnailPath))
                        //    {
                        //        var splits = thumbnailPath.Split(".");
                        //        splits[splits.Length-1] = "png";

                        //        thumbnailPath = string.Join(".", splits);

                        //        await img.SaveAsPngAsync(thumbnailPath);
                        //    }
                        //}
                    }

                }
                catch (Exception ex) //They love to close my connection sometimes
                {
                    await Task.Delay(5000);

                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        var contentType = response.Content.Headers.ContentType?.MediaType;

                        var extension = contentType switch
                        {
                            "image/jpeg" => "jpg",
                            "image/png" => "png",
                            "image/gif" => "gif",
                            "image/webp" => "webp"
                        };

                        thumbnailPath = Path.Combine(FolderPath, YoutubeExplodeClient.MakeValidFileName(id + "." + extension));

                        using (var s = await client.GetStreamAsync(url))
                        {
                            using (var fs = new FileStream(thumbnailPath, FileMode.OpenOrCreate))
                            {
                                await s.CopyToAsync(fs);
                            }
                        }

                        //if (extension != "png")
                        //{
                        //    using (var img = Image.Load(thumbnailPath))
                        //    {
                        //        var fi = new FileInfo(thumbnailPath);
                        //        var splits = thumbnailPath.Split(".");
                        //        splits[splits.Length - 1] = "png";

                        //        thumbnailPath = string.Join(".", splits);

                        //        await img.SaveAsPngAsync(thumbnailPath);
                        //    }
                        //}
                    }
                }
                
                Console.WriteLine($"Thumbnail done downloading: {thumbnailPath}");

                if (doProcessing)
                {
                    convertedThumbnailPath = Path.Combine(FolderPath, YoutubeExplodeClient.MakeValidFileName(id + ".png"));

                    var hasSolidBorders = YoutubeExplodeClient.HasSolidBorders(thumbnailPath);
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
                }
                else
                {
                    convertedThumbnailPath = thumbnailPath;
                }
            }
            catch (Exception ex)
            {
                return null;
            }

            return convertedThumbnailPath;
        }

        public static (string title, string artist) ManuallyParseVideoData(Video video)
        {
            var authorName = video.Author.ChannelTitle.Replace(" - Topic", "");
            var videoTitle = video.Title.ToLower().Replace("[official video]", "").Replace("music video", "").Replace("\"", "").Replace("| prime video", "").Replace("prime video", "").Replace("official music video", "")
                .Replace("official video", "").Replace("「", "").Replace("「", "」").Replace("official", "").Replace("music", "");
            
            videoTitle = Regex.Replace(videoTitle, @"\s*\(.*?\)\s*", "");

            // Remove [ ... ]
            videoTitle = Regex.Replace(videoTitle, @"\s*\[.*?\]\s*", "");

            var match = Regex.Match(videoTitle, @"^(.*?)\s*-\s*(.*)$");

            if (match.Success)
            {
                string artist = match.Groups[1].Value;
                string title = match.Groups[2].Value.Split(",").FirstOrDefault();

                videoTitle = title;
                authorName = artist;
            }
            else
            {

                videoTitle = videoTitle.Replace(authorName.ToLower(), "").Replace("-", "");
            }

            if (videoTitle.ToLower().Contains("from"))
            {
                var split = videoTitle.ToLower().Split("from");
                authorName = split.LastOrDefault();
                videoTitle = split.FirstOrDefault();
            }

            videoTitle = videoTitle.Trim();

            return (videoTitle, authorName);
        }

        public static bool HasSolidBorders(string imagePath)
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

        public static bool AreColorsSimilar(Rgba32 c1, Rgba32 c2, float threshold)
        {
            float rDiff = c1.R - c2.R;
            float gDiff = c1.G - c2.G;
            float bDiff = c1.B - c2.B;

            float distance = MathF.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);

            return distance < threshold;
        }

        public static bool IsSolidColor(Image<Rgba32> image, int startX, int startY, int width, int height)
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

        public async Task<List<ErrorModel>> ConvertFromQueryFileAsync(string path)
        {
            string[] Querys = File.ReadAllLines(path);
            var errors = new List<ErrorModel>();

            foreach (string query in Querys)
            {
                if (query.StartsWith('#'))
                    continue;

                var error = await ConvertFromVideoQueryAsync(query);

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

		public static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            var ret = System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");

            return ret;
		}

        public static string MakeTagName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            var ret = System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");

            ret = ret.Replace("(", "").Replace(")", "");

            //ret = ret.PadRight(10).Substring(0, 10).Trim();

            return ret;
        }
    }
}
