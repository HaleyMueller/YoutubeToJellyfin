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
                string videoURL = "";
                if (Uri.IsWellFormedUriString(videoURL, UriKind.Absolute))
                {
                    await youtubeExplodeClient.ConvertFromVideoURLAsync(videoURL);
                }

				string filePath = @"";
                if (File.Exists(filePath))
                {
					var errors = await youtubeExplodeClient.ConvertFromTextFileAsync(filePath);
					foreach (string error in errors)
					{
						Console.WriteLine(error);
					}
				}

                string playlistURL = "";
                if (Uri.IsWellFormedUriString(playlistURL, UriKind.Absolute))
                {
				    await youtubeExplodeClient.ConvertFromPlaylistURLAsync(playlistURL);
                }

				Console.WriteLine("Done :3");
            }).Wait();
        }	
    }
}
