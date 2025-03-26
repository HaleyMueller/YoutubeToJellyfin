using YoutubeToMusic.BLL;

namespace YoutubeToMusic.TestConsole
{
    internal class Program
    {
        static string FolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");

        static void Main(string[] args)
        {
            Selection? selection = null;

            Console.WriteLine("Youtube downloader extreme edition 😎");
            Console.WriteLine("------------------------");
            Console.WriteLine("");
            Console.WriteLine("Type a character for command");
            Console.WriteLine("");
            Console.WriteLine("------------------------");
            Console.WriteLine("");
            Console.WriteLine("P => Playlist");
            Console.WriteLine("V => Video");
            Console.WriteLine("F => File");
            Console.Write(">");

            while (selection == null) 
            {
                var input = Console.ReadLine();

                switch (input.ToUpper())
                {
                    case "P":
                    case "PLAYLIST":
                        selection = Selection.Playlist;
                        break;
                    case "V":
                    case "VIDEO":
                        selection = Selection.Video;
                        break;
                    case "F":
                    case "FILE":
                        selection = Selection.File;
                        break;
                    default:
                        Console.WriteLine("Wrong command. Please try again");
                        Console.Write(">");
                        break;
                }
            }

            Console.WriteLine($"You selected {selection.ToString()}");
            Console.Write($"Give me your link/file path :) >");

            var param = Console.ReadLine();

            YoutubeExplodeClient youtubeExplodeClient = new YoutubeExplodeClient(FolderPath);
            Task.Run(async () =>
            {
                var errors = new List<ErrorModel>();

                switch (selection)
                {
                    case Selection.Playlist:
                        if (Uri.IsWellFormedUriString(param, UriKind.Absolute))
                        {
                            errors = await youtubeExplodeClient.ConvertFromPlaylistURLAsync(param);
                        }
                        break;
                    case Selection.Video:
                        if (Uri.IsWellFormedUriString(param, UriKind.Absolute))
                        {
                            var errorModel = await youtubeExplodeClient.ConvertFromVideoURLAsync(param);

                            if (errorModel != null)
                            {
                                errors.Add(errorModel);
                            }
                        }
                        break;
                    case Selection.File:
                        if (File.Exists(param))
                        {
                            errors = await youtubeExplodeClient.ConvertFromTextFileAsync(param);
                        }
                        break;
                }

                Console.WriteLine("Done :3");

                foreach (var errorModel in errors)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(errorModel.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }).Wait();

            Console.WriteLine("Press any key once you finished with Picard scanning");
            Console.ReadKey();

            MusicBrainz _musicBrainz = new MusicBrainz();
            var errors = _musicBrainz.CreateDirectoriesAfterPicardWasScanned();

            foreach (var errorModel in errors) 
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorModel.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine("Done again :3"); 
            Console.ReadKey();
        }	

        enum Selection
        {
            Playlist,
            Video,
            File
        }
    }
}
