using AngleSharp.Dom;
using AngleSharp.Io;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib;
using TagLib.Matroska;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;
using YoutubeToMusic.DataEntities;
using static YoutubeToMusic.BLL.MusicBrainz;

namespace YoutubeToMusic.BLL
{
    public class VideoDownloader
    {
        private YoutubeExplodeClient _youtubeClient;
        private Fingerprinting _fingerprinter;
        private MusicBrainz _musicBrainz;
        private string FolderPath;

        public VideoDownloader(string folderPath)
        {
            _youtubeClient = new YoutubeExplodeClient(folderPath);
            _fingerprinter = new Fingerprinting("NV3PDyZXte");
            _musicBrainz = new MusicBrainz(folderPath);

            FolderPath = folderPath;
        }

        Random rand = new Random();

        public async Task<DataEntities.DataResponse<bool>> DownloadYoutubeVideo(YoutubeExplode.Videos.Video video)
        {
            Console.WriteLine("");

            var ret = new DataEntities.DataResponse<bool>(video.Title);

            string albumArtURL = "";
            string songTitle = "";
            List<string> artists = new List<string>();
            string album = "";
            string id = "";
            Dictionary<string, int> genres = new Dictionary<string, int>();
            TimeSpan duration = TimeSpan.MinValue;
            uint? track = null;
            uint? year = null;
            string MusicBrainzReleaseId = "", MusicBrainzTrackId = "", MusicBrainzReleaseArtistId = "";

            try
            {
                //first download video
                var audioFilePath = await _youtubeClient.DownloadVideo(video);

                if (audioFilePath.HasError)
                {
                    var r = new DataEntities.DataResponse<bool>();

                    r.ResponseMessages = DataEntities.DataResponse<bool>.CopyMessages(r);

                    return r;
                }

                var seconds = FFMPEG.GetDuration(audioFilePath.Data);
                duration = TimeSpan.FromSeconds(seconds);

                //fingerprint match it
                var fingerprint = await _fingerprinter.Fingerprint(audioFilePath.Data);

                if (fingerprint.HasError == false)
                {
                    if (fingerprint.Data != null && fingerprint.Data.results.Any())
                    {
                        Console.WriteLine("Was able to fingerprint song");

                        var bestRecording = await _musicBrainz.BestRecording(fingerprint.Data.results.FirstOrDefault().recordings.Select(x => x.id).ToList(), duration);

                        if (bestRecording.HasError == false)
                        {
                            songTitle = bestRecording.Data.Recording.Title;
                            album = bestRecording.Data.Release.Title;
                            artists = bestRecording.Data.Artists?.Select(x => x.Name).ToList();
                            albumArtURL = bestRecording.Data.AlbumArtUri;
                            genres = bestRecording.Data.Genres;
                            id = bestRecording.Data.Recording.Id.ToString();

                            if (bestRecording.Data.Recording.FirstReleaseDate != null && bestRecording.Data.Recording.FirstReleaseDate.Year != null)
                                year = uint.Parse(bestRecording.Data.Recording.FirstReleaseDate.Year.ToString());

                            MusicBrainzReleaseArtistId = bestRecording.Data.Artists.FirstOrDefault().Artist.Id.ToString();
                            MusicBrainzTrackId = bestRecording.Data.Recording.Id.ToString();
                            MusicBrainzReleaseId = bestRecording.Data.Release.Id.ToString();

                            if (bestRecording.Data.Track != null)
                                track = uint.Parse(bestRecording.Data.Track.Number);
                        }
                    }
                }

                //if not musicbrainz match it
                if (string.IsNullOrEmpty(songTitle))
                {
                    var musicBrainzLookup = await _musicBrainz.FindEntry(video);

                    if (musicBrainzLookup.HasError == false)
                    {
                        ret.AddWarning($"I was able to manually find song on musicbrainz by name lookup");
                        Console.WriteLine("Was able to manually find song on musicbrainz");

                        songTitle = musicBrainzLookup.Data.Recording.Title;
                        album = musicBrainzLookup.Data.Release.Title;
                        artists = musicBrainzLookup.Data.Recording.ArtistCredit.Select(x => x.Name).ToList();
                        albumArtURL = musicBrainzLookup.Data.AlbumArtUri;
                        genres = musicBrainzLookup.Data.Genres;
                        id = musicBrainzLookup.Data.Recording.Id.ToString();

                        if (musicBrainzLookup.Data.Recording.FirstReleaseDate != null && musicBrainzLookup.Data.Recording.FirstReleaseDate.Year != null)
                            year = uint.Parse(musicBrainzLookup.Data.Recording.FirstReleaseDate.Year.ToString());

                        MusicBrainzReleaseArtistId = musicBrainzLookup.Data.Artists.FirstOrDefault().Artist.Id.ToString();
                        MusicBrainzTrackId = musicBrainzLookup.Data.Recording.Id.ToString();
                        MusicBrainzReleaseId = musicBrainzLookup.Data.Release.Id.ToString();

                        if (musicBrainzLookup.Data.Track != null)
                            track = uint.Parse(musicBrainzLookup.Data.Track.Number);
                    }
                }

                //if not then manual
                if (string.IsNullOrEmpty(songTitle))
                {
                    (string authorName, string videoTitle) = YoutubeExplodeClient.ManuallyParseVideoData(video);

                    songTitle = videoTitle;
                    artists = new List<string>() { authorName };
                    album = "UNKNOWN";

                    ret.AddWarning($"I had to manually parse the name: Title: ${songTitle} Artist: {authorName} Album: {album}");
                }

                var thumbnailPath = "";
                if (string.IsNullOrEmpty(albumArtURL) == false) //get album from musicbrainz
                {
                    thumbnailPath = await YoutubeExplodeClient.DownloadThumbnail(albumArtURL, id.Replace("-", ""), FolderPath, "png", false);
                }

                if (string.IsNullOrEmpty(albumArtURL) || string.IsNullOrEmpty(thumbnailPath)) //no album then get from youtube thumbnail
                {
                    albumArtURL = video.Thumbnails.GetWithHighestResolution().Url;
                    thumbnailPath = await YoutubeExplodeClient.DownloadThumbnail(albumArtURL, id.Replace("-", ""), FolderPath, albumArtURL.Split('.').Last(), true);
                }

                //taglib
                Console.WriteLine($"Tagging file: {audioFilePath.Data}");
                var file = TagLib.File.Create(audioFilePath.Data);
                file.Tag.Title = YoutubeExplodeClient.MakeTagName(songTitle);
                file.Tag.Performers = artists.ToArray();
                file.Tag.Album = YoutubeExplodeClient.MakeTagName(album);

                if (string.IsNullOrEmpty(MusicBrainzReleaseId) == false)
                    file.Tag.MusicBrainzReleaseId = MusicBrainzReleaseId;
                if (string.IsNullOrEmpty(MusicBrainzTrackId) == false)
                    file.Tag.MusicBrainzTrackId = MusicBrainzTrackId;
                if (string.IsNullOrEmpty(MusicBrainzReleaseArtistId) == false) 
                { 
                    file.Tag.MusicBrainzReleaseArtistId = MusicBrainzReleaseArtistId;
                    file.Tag.MusicBrainzArtistId = MusicBrainzReleaseArtistId;
                }

                if (track.HasValue)
                    file.Tag.Track = track.Value;

                Picture picture = new Picture();
                picture.Type = PictureType.FrontCover;
                picture.MimeType = "image/png";
                picture.Description = "Cover";
                picture.Data = ByteVector.FromPath(thumbnailPath);

                file.Tag.Pictures = new TagLib.Picture[] { picture };

                if (genres != null)
                    file.Tag.Genres = genres.Where(x => x.Value >= 0).Select(x => x.Key).ToArray();

                if (year.HasValue)
                    file.Tag.Year = year.Value;

                file.Save();
                System.IO.File.Delete(thumbnailPath);
                Console.WriteLine($"File tagged: {file.Tag.Title} {file.Tag.Performers?.FirstOrDefault()}");

                //Rename file
                //System.IO.File.Move(audioFilePath.Data, Path.Combine(FolderPath, YoutubeExplodeClient.MakeValidFileName($"{songTitle}.ogg")));

                //move to folders
            }
            catch (Exception ex)
            {
                ret.AddException(ex);
            }

            ret.Data = true;

            return ret;
        }

        public async Task<DataEntities.DataResponse<bool>> MetaDataFolder(string path)
        {
            var ret = new DataEntities.DataResponse<bool>(path);

            try
            {
                var files = System.IO.Directory.GetFiles(path);

                foreach (var file in files)
                {
                    var response = await MetaDataFile(file);
                    ret.ResponseMessages.AddRange(DataResponse<bool>.CopyMessages(response));
                }
            }
            catch (Exception ex)
            {
                ret.AddException(ex);
            }

            return ret;
        }

        public async Task<DataEntities.DataResponse<bool>> MetaDataFile(string filePath) 
        {
            var ret = new DataEntities.DataResponse<bool>(filePath);

            string albumArtURL = "";
            string songTitle = "";
            List<string> artists = new List<string>();
            string album = "";
            string id = "";
            Dictionary<string, int> genres = new Dictionary<string, int>();
            TimeSpan duration = TimeSpan.MinValue;
            uint? track = null;
            uint? year = null;
            string MusicBrainzReleaseId = "", MusicBrainzTrackId = "", MusicBrainzReleaseArtistId = "";

            try
            {
                Console.WriteLine("");
                Console.WriteLine($"Starting metadata for: {filePath}");

                var seconds = FFMPEG.GetDuration(filePath);
                duration = TimeSpan.FromSeconds(seconds);

                //fingerprint match it
                var fingerprint = await _fingerprinter.Fingerprint(filePath);

                if (fingerprint.HasError == false)
                {
                    if (fingerprint.Data != null && fingerprint.Data.results.Any())
                    {
                        Console.WriteLine($"Fingerprint{(fingerprint.Data.results.Count() > 1 ? "s" : "")} found: {fingerprint.Data.results.Count()}");

                        var bestRecording = await _musicBrainz.BestRecording(fingerprint.Data.results.FirstOrDefault().recordings.Select(x => x.id).ToList(), duration);

                        if (bestRecording.HasError == false)
                        {
                            songTitle = bestRecording.Data.Recording.Title;
                            album = bestRecording.Data.Release.Title;
                            artists = bestRecording.Data.Artists?.Select(x => x.Name).ToList();
                            albumArtURL = bestRecording.Data.AlbumArtUri;
                            genres = bestRecording.Data.Genres;
                            id = bestRecording.Data.Recording.Id.ToString();

                            if (bestRecording.Data.Recording.FirstReleaseDate != null && bestRecording.Data.Recording.FirstReleaseDate.Year != null)
                                year = uint.Parse(bestRecording.Data.Recording.FirstReleaseDate.Year.ToString());

                            MusicBrainzReleaseArtistId = bestRecording.Data.Artists.FirstOrDefault().Artist.Id.ToString();
                            MusicBrainzTrackId = bestRecording.Data.Recording.Id.ToString();
                            MusicBrainzReleaseId = bestRecording.Data.Release.Id.ToString();

                            if (bestRecording.Data.Track != null)
                                track = uint.Parse(bestRecording.Data.Track.Number);


                            var thumbnailPath = "";
                            if (string.IsNullOrEmpty(albumArtURL) == false) //get album from musicbrainz
                            {
                                thumbnailPath = await YoutubeExplodeClient.DownloadThumbnail(albumArtURL, id.Replace("-", ""), FolderPath, "png", false);
                            }

                            //taglib
                            Console.WriteLine($"Tagging file: {filePath}");
                            var file = TagLib.File.Create(filePath);
                            file.Tag.Title = YoutubeExplodeClient.MakeTagName(songTitle);
                            file.Tag.Performers = artists.ToArray();
                            file.Tag.Album = YoutubeExplodeClient.MakeTagName(album);

                            if (string.IsNullOrEmpty(MusicBrainzReleaseId) == false)
                                file.Tag.MusicBrainzReleaseId = MusicBrainzReleaseId;
                            if (string.IsNullOrEmpty(MusicBrainzTrackId) == false)
                                file.Tag.MusicBrainzTrackId = MusicBrainzTrackId;
                            if (string.IsNullOrEmpty(MusicBrainzReleaseArtistId) == false)
                            {
                                file.Tag.MusicBrainzReleaseArtistId = MusicBrainzReleaseArtistId;
                                file.Tag.MusicBrainzArtistId = MusicBrainzReleaseArtistId;
                            }

                            if (track.HasValue)
                                file.Tag.Track = track.Value;

                            if (string.IsNullOrEmpty(thumbnailPath) == false)
                            {
                                Picture picture = new Picture();
                                picture.Type = PictureType.FrontCover;
                                picture.MimeType = "image/png";
                                picture.Description = "Cover";
                                picture.Data = ByteVector.FromPath(thumbnailPath);

                                file.Tag.Pictures = new TagLib.Picture[] { picture };
                            }

                            if (genres != null)
                                file.Tag.Genres = genres.Where(x => x.Value >= 0).Select(x => x.Key).ToArray();

                            if (year.HasValue)
                                file.Tag.Year = year.Value;

                            file.Save();
                            System.IO.File.Delete(thumbnailPath);
                            Console.WriteLine($"File tagged: {file.Tag.Title} {file.Tag.Performers?.FirstOrDefault()}");
                        }
                        else
                            Console.WriteLine($"Fingerprint error. Couldn't find best recording. Skipping {filePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                return ret.AddException(ex);
            }

            return ret;
        }

        public async Task<DataEntities.DataResponse<bool>> ConvertFromVideoURLAsync(string URL)
        {
            var ret = new DataEntities.DataResponse<bool>(URL);
            try
            {
                Console.WriteLine("");
                Console.WriteLine($"Starting download for: {URL}");

                var video = await _youtubeClient._client.Videos.GetAsync(URL);

                ret = await DownloadYoutubeVideo(video);
            }
            catch (Exception ex)
            {
                return new DataEntities.DataResponse<bool>().AddException(ex);
            }

            return ret;
        }

        public async Task<DataEntities.DataResponse<bool>> ConvertFromTextFileAsync(string path)
        {
            var ret = new DataEntities.DataResponse<bool>(path);
            
            try
            {
                string[] URLs = System.IO.File.ReadAllLines(path);

                foreach (string URL in URLs)
                {
                    if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute))
                    {
                        ret.AddError($"Incorrect URI format for: {URL}");
                        continue;
                    }

                    var response = await ConvertFromVideoURLAsync(URL);

                    ret.ResponseMessages = DataEntities.DataResponse<bool>.CopyMessages(response);
                }

                return ret;
            }
            catch(Exception ex)
            {
                return ret.AddException(ex);
            }
        }

        public async Task<DataEntities.DataResponse<bool>> ConvertFromVideoQueryAsync(string query)
        {
            Console.WriteLine("");
            Console.WriteLine($"Query: {query}");

            try
            {
                var videos = new List<YoutubeExplode.Search.VideoSearchResult>();

                int count = 0;
                await foreach (var result in _youtubeClient._client.Search.GetVideosAsync(query))
                {
                    videos.Add(result);

                    count++;
                    if (count >= 20)
                        break;
                }

                if (videos == null || videos.Count <= 0)
                {
                    Console.WriteLine($"Couldn't find video for query: {query}");
                    return new DataEntities.DataResponse<bool>().AddError($"Couldn't find video for query: {query}");
                }

                var availableVideos = videos.Where(x => x.Title.ToUpper().Contains("AMV") == false && x.Title.ToUpper().Contains("FIRST TAKE") == false && x.Title.ToUpper().Contains("SHORTS") == false && x.Title.ToUpper().Contains("ASMV") == false && x.Title.ToUpper().Contains("FULL") == false && x.Title.ToUpper().Contains("LYRIC") == false && x.Duration > TimeSpan.FromSeconds(60));

                var video = await _youtubeClient._client.Videos.GetAsync(availableVideos.FirstOrDefault().Id);
                return await DownloadYoutubeVideo(video);
            }
            catch (Exception ex)
            {
                return new DataEntities.DataResponse<bool>().AddException(ex);
            }
        }

        public async Task<DataEntities.DataResponse<bool>> ConvertFromPlaylistURLAsync(string URL)
        {
            var ret = new DataEntities.DataResponse<bool>(URL);

            try
            {
                var videos = await _youtubeClient._client.Playlists.GetVideosAsync(URL);

                foreach (PlaylistVideo video in videos)
                {
                    var response = await ConvertFromVideoURLAsync(video.Url);

                    ret.ResponseMessages = DataEntities.DataResponse<bool>.CopyMessages(response);
                }

                return ret;
            }
            catch (Exception ex)
            {
                return ret.AddException(ex);
            }
        }

        public async Task<DataEntities.DataResponse<bool>> ConvertFromQueryFileAsync(string path)
        {
            var ret = new DataEntities.DataResponse<bool>(path);

            try
            {
                string[] Querys = System.IO.File.ReadAllLines(path);

                foreach (string query in Querys)
                {
                    if (query.StartsWith('#'))
                        continue;

                    var response = await ConvertFromVideoQueryAsync(query);

                    ret.ResponseMessages = DataEntities.DataResponse<bool>.CopyMessages(response);
                }

                return ret;
            }
            catch (Exception ex)
            {
                return ret.AddException(ex);
            }
        }
    }
}
