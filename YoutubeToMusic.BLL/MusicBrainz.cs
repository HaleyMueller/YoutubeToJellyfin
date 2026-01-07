using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using YoutubeToMusic.DataEntities;

namespace YoutubeToMusic.BLL
{
    public class MusicBrainz
    {
        //HttpClient _httpClient;
        public string FolderPath { get; private set; }
        public MusicBrainz(string folderPath)
        {
            FolderPath = folderPath;
            //_httpClient = new HttpClient();
        }

        public List<TestUrl> TestUrls = new List<TestUrl>()
        {
            new TestUrl() { URL = "https://www.youtube.com/watch?v=yzC4hFK5P3g" },
            new TestUrl() { URL = "https://www.youtube.com/watch?v=K3m3_7RoGZk" },
            new TestUrl() { URL = "https://www.youtube.com/watch?v=_mkiGMtbrPM" },
            new TestUrl() { URL = "https://www.youtube.com/watch?v=SXJGTnVfJic" },
        };

        private Query _client = new Query("Auto-Music-Tagger", "0.0.1", "crownedhaley@gmail.com");

        public class TestUrl
        {
            public string URL { get; set; }
            public string Title { get; set; }
            public string ChannelName { get; set; }
        }

        public async Task<DataEntities.DataResponse<RecordingRelease>> BestRecording(List<Guid> musicbrainzID, TimeSpan duration)
        {
            var ret = new DataEntities.DataResponse<RecordingRelease>();
            
            try
            {
                var query = $"rid:{musicbrainzID.ToString()}";
                //var query = $"rid:{musicbrainzID.ToString().Replace("-", "")}";

                List<IRecording> recordings = new List<IRecording>();

                foreach (var id in musicbrainzID)
                {
                    try
                    {
                        var recording = await _client.LookupRecordingAsync(id, Include.Releases | Include.Genres | Include.Artists | Include.Media);

                        recordings.Add(recording);
                    }
                    catch (Exception ex) { }
                }

                if (recordings.Any())
                {
                    var best = await BestRecording(recordings, duration);

                    //var bestGenreRecording = await _client.LookupRecordingAsync(best.Recording.Id, Include.Genres);
                    //var bestArtistRecording = await _client.LookupRecordingAsync(best.Recording.Id, Include.Artists);

                    best.Genres = await GetGenres(best.Recording);
                    best.Artists = best.Recording.ArtistCredit;

                    foreach (var medium in best.Release?.Media)
                    {
                        if (best.Track != null)
                            continue;

                        foreach (var track in medium?.Tracks)
                        {
                            if (track.Title == best.Recording.Title)
                            {
                                best.Track = track;
                                break;
                            }
                        }
                    }

                    ret.Data = best;
                }
                else
                {
                    ret.AddError($"No recordings found for: rid:{musicbrainzID.ToString().Replace("-", "")}");
                }
            }
            catch (Exception ex)
            {
                ret.AddException(ex);
            }

            return ret;
        }

        public async Task<DataEntities.DataResponse<RecordingRelease>> FindEntry(YoutubeExplode.Videos.Video video)
        {
            var ret = new DataEntities.DataResponse<RecordingRelease>();

            try
            {
                Console.WriteLine($"Parsing data from this information: {video.Author.ChannelTitle}, {video.Title}");

                (string authorName, string videoTitle) = YoutubeExplodeClient.ManuallyParseVideoData(video);

                Console.WriteLine($"Finding entry in MusicBrainz: Author: {authorName} Title: {video.Title}");

                var query = $@"recording:'" + videoTitle + "' AND artist:'" + authorName + "'";

                var rrr = await _client.FindRecordingsAsync(query, limit: 5, simple: true);


                if (rrr.TotalResults <= 0)
                {
                    if (rrr.TotalResults <= 0)
                    {
                        var test = videoTitle.Split(":");

                        if (test.Length >= 2)
                        {
                            query = $@"recording:'" + test[1].Trim() + "' AND artist:'" + test[0].Trim() + "'";
                            //query = $@"recording:""{test[1].Trim()}"" AND artist:""{test[0].Trim()}""";
                            rrr = await _client.FindRecordingsAsync(query, limit: 50, simple: true);
                        }
                    }
                }

                var best = await BestRecording(rrr.Results.ToList());
                var gre = await GetGenres(best.Recording);

                foreach (var medium in best.Release?.Media)
                {
                    if (best.Track != null)
                        continue;

                    foreach (var track in medium?.Tracks)
                    {
                        if (track.Title == best.Recording.Title)
                        {
                            best.Track = track;
                            break;
                        }
                    }
                }

                Console.WriteLine($"Found {rrr.TotalResults} recordings");

                Console.WriteLine($"Best recording picked: Score: {best.Score} Title: {best.Recording.Title} Album: {best.Release.Title} Artists: {string.Join(",", best.Recording.ArtistCredit.Select(x => x.Name))} Album Url: {best.AlbumArtUri}");

                ret.Data = best;
            }
            catch (Exception ex) 
            { 
                ret.AddException(ex);
            }

            return ret;
        }

        public async Task<Dictionary<string, int>> GetGenres(List<IRecording> recordings)
        {
            var r = new Dictionary<string, int>();

            foreach (var recording in recordings)
            {
                if (recording != null)
                {
                    if (recording.Genres != null)
                    {
                        foreach (var genre in recording.Genres)
                        {
                            if (r.TryGetValue(genre.Name, out int voteCount))
                            {
                                r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                            }
                            else
                            {
                                r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                            }
                        }

                    }

                    if (recording.UserGenres != null)
                        foreach (var genre in recording.UserGenres)
                        {
                            if (r.TryGetValue(genre.Name, out int voteCount))
                            {
                                r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                            }
                            else
                            {
                                r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                            }
                        }

                    if (recording.Tags != null)
                        foreach (var genre in recording.Tags)
                        {
                            if (r.TryGetValue(genre.Name, out int voteCount))
                            {
                                r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                            }
                            else
                            {
                                r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                            }
                        }

                    if (recording.UserTags != null)
                        foreach (var genre in recording.UserTags)
                        {
                            if (r.TryGetValue(genre.Name, out int voteCount))
                            {
                                r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                            }
                            else
                            {
                                r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                            }
                        }

                }
            }

            return r;
        }

        public async Task<Dictionary<string, int>> GetGenres(IRecording recording)
        {
            var r = new Dictionary<string, int>();

            if (recording != null)
            {
                if (recording.Genres != null)
                {
                    foreach (var genre in recording.Genres)
                    {
                        if (r.TryGetValue(genre.Name, out int voteCount))
                        {
                            r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                        }
                        else
                        {
                            r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                        }
                    }

                }

                if (recording.UserGenres != null)
                    foreach (var genre in recording.UserGenres)
                    {
                        if (r.TryGetValue(genre.Name, out int voteCount))
                        {
                            r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                        }
                        else
                        {
                            r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                        }
                    }

                if (recording.Tags != null)
                    foreach (var genre in recording.Tags)
                    {
                        if (r.TryGetValue(genre.Name, out int voteCount))
                        {
                            r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                        }
                        else
                        {
                            r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                        }
                    }

                if (recording.UserTags != null)
                    foreach (var genre in recording.UserTags)
                    {
                        if (r.TryGetValue(genre.Name, out int voteCount))
                        {
                            r[genre.Name] += genre.VoteCount.GetValueOrDefault();
                        }
                        else
                        {
                            r[genre.Name] = genre.VoteCount.GetValueOrDefault();
                        }
                    }

            }

            return r;
        }

        public class RecordingRelease
        {
            public int Score { get; set; }
            public IRecording Recording { get; set; }
            public IRelease Release { get; set; }
            public IReadOnlyList<INameCredit> Artists { get; set; }
            public string AlbumArtUri { get; set; }
            public Dictionary<string, int> Genres { get; set; }
            public ITrack Track { get; set; }
        }

        public async Task<RecordingRelease> BestRecording(List<IRecording> recordings, TimeSpan? duration = null)
        {
            var orderedRecordings = new List<IRecording>();

            if (duration.HasValue)
            {
                 orderedRecordings = recordings.OrderBy(x => Math.Abs((x.Length - duration).GetValueOrDefault().Ticks)).ThenBy(x => x.FirstReleaseDate).ToList();
            }
            else
            {
                orderedRecordings = recordings.OrderBy(x => x.FirstReleaseDate).ToList();
            }

            foreach (var recording in orderedRecordings)
            {
                var orderedReleases = recording.Releases?.OrderBy(x => x.Date).Where(x => x.Status?.ToLower() != "promo") ?? new List<IRelease>();

                foreach (var release in orderedReleases)
                {
                    if (release.ArtistCredit != null && release.ArtistCredit.Any(x => x.Name.ToLower().Contains("various")))
                        continue;

                    if (release.Status?.ToLower() == "bootleg")
                        continue;

                    var url = $"https://coverartarchive.org/release/{release.Id}/front";

                    var hasArt = await IsUrl200HeadAsync(url);

                    if (hasArt)
                        return new RecordingRelease() { Release = release, Recording = recording, AlbumArtUri = url };
                }
            }

            Console.WriteLine("Couldn't find album art");

            foreach (var recording in orderedRecordings)
            {
                var orderedReleases = recording.Releases?.OrderBy(x => x.Date).Where(x => x.Status?.ToLower() != "promo") ?? new List<IRelease>();

                foreach (var release in orderedReleases)
                {
                    if (release.ArtistCredit != null && release.ArtistCredit.Any(x => x.Name.ToLower().Contains("various")))
                        continue;

                    if (release.Status?.ToLower() == "bootleg")
                        continue;

                    return new RecordingRelease() { Release = release, Recording = recording, AlbumArtUri = null };
                }
            }

            return null;
        }

        public async Task<RecordingRelease> BestRecording(List<ISearchResult<IRecording>> recordings)
        {
            //todo inject score back in
            return await BestRecording(recordings.OrderByDescending(x => x.Score).Where(x => x.Score >= 70).Select(x => x.Item).ToList());
        }

        public class Genre : IGenre
        {
            public int? VoteCount { get; set; }

            public string? Disambiguation { get; set; }

            public string? Name { get; set; }

            public EntityType EntityType { get; set; }

            public Guid Id { get; set; }

            public IReadOnlyDictionary<string, object?>? UnhandledProperties { get; set; }
        }

        public static async Task<bool> IsUrl200HeadAsync(string url)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, url);

            try
            {
                using var response = await client.SendAsync(request);
                var r = await response.Content.ReadAsStringAsync();

                if (r.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return false;

                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }


        public DataEntities.DataResponse<bool> CreateDirectoriesAfterPicardWasScanned()
        {
            DataEntities.DataResponse<bool> ret = new DataEntities.DataResponse<bool>(FolderPath);

            foreach (var filePath in Directory.GetFiles(FolderPath))
            {
                try
                {
                    var file = TagLib.File.Create(filePath);
                    Console.WriteLine($"File: {filePath}");
                    Console.WriteLine($"Artist: {file.Tag.FirstPerformer}");
                    Console.WriteLine($"Album: {file.Tag.Album}");

                    if (string.IsNullOrEmpty(file.Tag.FirstPerformer)) 
                    {
                        ret.AddError("No performer was tagged on the file. Skipping...");
                        continue;
                    }

                    if (string.IsNullOrEmpty(file.Tag.Album))
                    {
                        ret.AddError("No album was tagged on the file. Skipping...");
                        continue;
                    }

                    var artistPath = MakeValidDirName(Path.Combine(FolderPath, string.Join(", ", file.Tag.Performers)).Trim());
                    var albumPath = MakeValidDirName(Path.Combine(artistPath, file.Tag.Album));
                    var songPath = Path.Combine(albumPath, MakeValidFileName(file.Tag.Title + "." + filePath.Split('.').Last()));

                    var nfoSongPath = Path.Combine(albumPath, MakeValidFileName(file.Tag.Title + ".nfo"));
                    var nfoArtistPath = Path.Combine(artistPath, "artist.nfo");

                    if (System.IO.Directory.Exists(artistPath) == false)
                    {
                        Directory.CreateDirectory(artistPath);
                    }

                    if (System.IO.Directory.Exists(albumPath) == false)
                    {
                        Directory.CreateDirectory(albumPath);
                    }

                    NfoGenerator.CreateArtistNfo(file, nfoArtistPath);
                    //NfoGenerator.CreateMusicVideoNfo(filePath, nfoSongPath);

                    File.Copy(filePath, songPath, true);
                }
                catch (Exception ex)
                {
                    ret.AddException(ex);
                }
            }

            return ret;
        }

        private static string CreateNFOSongFile()
        {
            var ret = new StringBuilder();



            return ret.ToString();
        }

        private static string MakeValidFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                name = name.Replace(c, '_'); // replace with underscore or something safe
            }


            var replacements = new Dictionary<char, string>
    {
        { ':', " -" },   // Album: Name -> Album - Name
        { '"', "'" },    // Replace quotes with apostrophe
        { '?', "" },     // Drop question marks
        { '*', "" },     // Drop asterisks
        { '<', "(" },    // Replace < with (
        { '>', ")" },    // Replace > with )
        { '|', "-" },    // Replace | with dash
        { '/', "-" },    // Replace slashes with dash
        { '\\', "-" }    // Replace backslashes with dash
    };

            foreach (var kvp in replacements)
            {
                name = name.Replace(kvp.Key.ToString(), kvp.Value);
            }

            return name;
        }

        private static string MakeValidDirName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            // Detect drive prefix (like C:\)
            string drive = "";
            string rest = path;

            if (Path.IsPathRooted(path) && path.Length > 2 && path[1] == ':')
            {
                drive = path.Substring(0, 2);   // e.g. "C:"
                rest = path.Substring(2);       // everything after the drive
            }

            // Replace all other colons in the remaining path
            rest = rest.Replace(":", " -");

            // Optionally handle other invalid chars
            foreach (var c in Path.GetInvalidPathChars())
            {
                if (c == ':') continue; // we already handled colons
                rest = rest.Replace(c.ToString(), "");
            }

            var replacements = new Dictionary<char, string>
            {
                { '"', "_" },    // Replace quotes with apostrophe
                { '?', "" },     // Drop question marks
                { '*', "" },     // Drop asterisks
                { '<', "(" },    // Replace < with (
                { '>', ")" },    // Replace > with )
                { '|', "-" },    // Replace | with dash
            };

            foreach (var kvp in replacements)
            {
                rest = rest.Replace(kvp.Key.ToString(), kvp.Value);
                drive = drive.Replace(kvp.Key.ToString(), kvp.Value);
            }


            return drive + rest;
        }

        //public async Task<string> GetMusicBrainzId(string title, string artist)
        //{
        //    title = title.ToUpper().Replace("\"", "").Replace("MUSIC VIDEO", "");

        //    if (title.Contains("|"))
        //    {
        //        var index = title.IndexOf('|');

        //        title = title.Substring(0, index);
        //    }

        //    artist = artist.Replace("\"", "");

        //    string query = $"https://musicbrainz.org/ws/2/recording/?query=recording:\"{Uri.EscapeDataString(title)}\" AND artist:\"{Uri.EscapeDataString(artist)}\"&fmt=json";

        //    var q = new Query("Chrome", "19.99", "mailto:milton.waddams@initech.com");

        //    var actualQuery = $"single AND {title}";
        //    //var actualQuery = $"recording:\"{title}\"";
        //    //actualQuery = Uri.EscapeDataString(actualQuery);

        //    var qq = await q.FindRecordingsAsync(actualQuery);

        //    HttpResponseMessage response = await _httpClient.GetAsync(query);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        Console.WriteLine($"Error: {response.StatusCode}");
        //        return null;
        //    }

        //    string json = await response.Content.ReadAsStringAsync();
        //    using JsonDocument doc = JsonDocument.Parse(json);
        //    JsonElement root = doc.RootElement;

        //    if (root.TryGetProperty("recordings", out JsonElement recordings) && recordings.GetArrayLength() > 0)
        //    {
        //        return recordings[0].GetProperty("id").GetString(); // First result's MusicBrainz ID
        //    }

        //    return null;
        //}
    }
}
