using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeToMusic.BLL
{
    public class YoutubeExplodeClient
    {
        private YoutubeClient _client = new YoutubeClient();

        public YoutubeExplodeClient()
        {
            _client = new YoutubeClient();
        }

        public async Task ConvertFromURLAsync(string URL)
        {
            var video = await _client.Videos.GetAsync(URL);

            var streamManifest = await _client.Videos.Streams.GetManifestAsync(URL);
            var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var stream = await _client.Videos.Streams.GetAsync(streamInfo);

            await _client.Videos.Streams.DownloadAsync(streamInfo, $"{video.Title}.{streamInfo.Container}");
        }
    }
}
