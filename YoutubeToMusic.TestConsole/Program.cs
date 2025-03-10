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
                await youtubeExplodeClient.ConvertFromURLAsync("https://www.youtube.com/watch?v=PCp2iXA1uLE");

                Console.WriteLine("Done :3");
            }).Wait();
        }
    }
}
