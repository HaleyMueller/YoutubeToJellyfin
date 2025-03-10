using AngleSharp.Dom;
using YoutubeToMusic.BLL;

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
				//    await youtubeExplodeClient.ConvertFromURLAsync("https://www.youtube.com/watch?v=s1iBYOEnKhM");
				string file = @"C:\Users\colli\Downloads\SomeSongs.txt";

				// Store each line in array of strings 
				string[] URLs = File.ReadAllLines(file);
				List<string> errors = new();

				foreach (string URL in URLs)
				{
					if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute))
					{
						errors.Add(URL);
						continue;
					}
					await youtubeExplodeClient.ConvertFromURLAsync(URL);
				}

				foreach(string error in errors)
				{
					Console.WriteLine(error);
				}
				Console.WriteLine("Done :3");
            }).Wait();
        }	
    }
}
