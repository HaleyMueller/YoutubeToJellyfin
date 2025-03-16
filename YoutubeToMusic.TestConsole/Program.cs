using AngleSharp.Dom;
using YoutubeToMusic.BLL;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace YoutubeToMusic.TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            YoutubeExplodeClient youtubeExplodeClient = new YoutubeExplodeClient();
            Task.Run(async () =>
            {
                string videoURL = "https://www.youtube.com/watch?v=5mhVBxfIuKw";
                if (Uri.IsWellFormedUriString(videoURL, UriKind.Absolute))
                {
                    await youtubeExplodeClient.ConvertFromVideoURLAsync(videoURL);
                }

                //string filePath = @"";
                //            if (File.Exists(filePath))
                //            {
                //	var errors = await youtubeExplodeClient.ConvertFromTextFileAsync(filePath);
                //	foreach (string error in errors)
                //	{
                //		Console.WriteLine(error);
                //	}
                //}

                //        string playlistURL = "https://www.youtube.com/watch?v=NIioTc_Yz_0&list=PL_s6uLLEod9SEyueU6E5-d5uEb5qccAGJ&pp=gAQB";
                //        if (Uri.IsWellFormedUriString(playlistURL, UriKind.Absolute))
                //        {
                //await youtubeExplodeClient.ConvertFromPlaylistURLAsync(playlistURL);
                //        }

                Console.WriteLine("Done :3");
            }).Wait();

            Console.WriteLine("Press any key once you finished with Picard scanning");
            Console.ReadKey();
            MusicBrainz _musicBrainz = new MusicBrainz();
            _musicBrainz.CreateDirectoriesAfterPicardWasScanned();
            Console.WriteLine("Done again :3"); 
            Console.ReadKey();
        }	
    }
}
