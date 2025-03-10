using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMusic.BLL
{
    public class YoutubeExplodeClient
    {
        private YoutubeClient _client = new YoutubeClient();
        private string folderPath = @"C:\Users\Haley\Downloads";
        private MusicBrainz _musicBrainz = new MusicBrainz();

		public YoutubeExplodeClient()
        {
            _client = new YoutubeClient();
            _musicBrainz = new MusicBrainz();
        }

        public async Task ConvertFromVideoURLAsync(string URL)
        {
			var video = await _client.Videos.GetAsync(URL);

            var streamManifest = await _client.Videos.Streams.GetManifestAsync(URL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            var fullPath = Path.Combine(folderPath, MakeValidFileName($"{video.Title}.{streamInfo.Container}"));

			await _client.Videos.Streams.DownloadAsync(streamInfo, fullPath);

            FFMPEG.ConvertFile(fullPath, Path.Combine(folderPath, MakeValidFileName($"{video.Title}.ogg")));

            await _musicBrainz.GetMusicBrainzId(video.Title, video.Author.ChannelTitle);
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
