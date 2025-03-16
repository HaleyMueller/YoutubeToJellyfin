using MetaBrainz.MusicBrainz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YoutubeToMusic.BLL
{
    public class MusicBrainz
    {
        //HttpClient _httpClient;
        public MusicBrainz()
        {
            //_httpClient = new HttpClient();
        }

        public void CreateDirectoriesAfterPicardWasScanned()
        {
            var folder = @"C:\Users\Haley\Desktop\test";

            foreach (var filePath in Directory.GetFiles(folder))
            {
                try
                {
                    var file = TagLib.File.Create(filePath);
                    Console.WriteLine($"Artist: {file.Tag.FirstPerformer}");
                    Console.WriteLine($"Album: {file.Tag.Album}");

                    var artistPath = Path.Combine(folder, string.Join(", ", file.Tag.Performers)).Trim();
                    var albumPath = Path.Combine(artistPath, file.Tag.Album);
                    var songPath = Path.Combine(albumPath, file.Tag.Title + "." + filePath.Split('.').Last());

                    if (System.IO.Directory.Exists(artistPath) == false)
                    {
                        Directory.CreateDirectory(artistPath);
                    }

                    if (System.IO.Directory.Exists(albumPath) == false)
                    {
                        Directory.CreateDirectory(albumPath);
                    }

                    File.Copy(filePath, songPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
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
