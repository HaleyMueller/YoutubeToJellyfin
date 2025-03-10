using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMusic.BLL
{
    public class YoutubeExplodeClient
    {
        private YoutubeClient _client = new YoutubeClient();
        private string folderPath = @"C:\Users\colli\Downloads";
        private MusicBrainz _musicBrainz = new MusicBrainz();

		public YoutubeExplodeClient()
        {
            _client = new YoutubeClient();
            _musicBrainz = new MusicBrainz();
        }

        public async Task ConvertFromURLAsync(string URL)
        {
			var video = await _client.Videos.GetAsync(URL);

            var streamManifest = await _client.Videos.Streams.GetManifestAsync(URL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var stream = await _client.Videos.Streams.GetAsync(streamInfo);

            var fullPath = Path.Combine(folderPath, MakeValidFileName($"{video.Title}.{streamInfo.Container}"));

			await _client.Videos.Streams.DownloadAsync(streamInfo, fullPath);

            FFMPEG.ConvertFile(filePath, $"{video.Title}.ogg");

            await _musicBrainz.GetMusicBrainzId(video.Title, video.Author.ChannelTitle);
        }

		private static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
		}
	}
}
