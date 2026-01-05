using AngleSharp.Io;
using YoutubeToMusic.BLL;

namespace YoutubeToMusic.TestConsole
{
    internal class Program
    {
        static string FolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");

        static Selection? ParseSelection(string arg) => arg.ToLower() switch
        {
            "--playlist" or "-p" => Selection.Playlist,
            "--video" or "-v" => Selection.Video,
            "--query" or "-q" => Selection.Query,
            "--queryfile" or "-qf" => Selection.QueryFile,
            "--file" or "-f" => Selection.File,
            "--create-folders" or "-cf" => Selection.CreateFolders,
            _ => null
        };

        static void PrintUsage()
        {
            Console.WriteLine("""
                Usage:
                  -p  <playlistUrl>
                  -v  <videoUrl>
                  -q  "search query"
                  -qf <queryFile.txt>
                  -f  <urls.txt>
                  -cf 
                """);
        }

        static void Main(string[] args)
        {
            System.Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (Path.Exists(FolderPath) == false)
                Directory.CreateDirectory(FolderPath);

            if (args.Length == 1 && (args[0] == "--create-folders" || args[0] == "-cf"))
            {

            }
            else if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            Selection? selection = ParseSelection(args.FirstOrDefault());
            if (selection == null)
            {
                Console.WriteLine($"Unknown flag: {args[0]}");
                PrintUsage();
                return;
            }

            string param = string.Join(" ", args.Skip(1).Take(1));

            YoutubeExplodeClient youtubeExplodeClient = new YoutubeExplodeClient(FolderPath);
            VideoDownloader videoDownloader = new VideoDownloader(FolderPath);
            Task.Run(async () =>
            {
                var response = new DataEntities.DataResponse<bool>();

                switch (selection)
                {
                    case Selection.Playlist:
                        if (Uri.IsWellFormedUriString(param, UriKind.Absolute))
                        {
                            response = await videoDownloader.ConvertFromPlaylistURLAsync(param);
                        }
                        break;
                    case Selection.Video:
                        if (Uri.IsWellFormedUriString(param, UriKind.Absolute))
                        {
                            response = await videoDownloader.ConvertFromVideoURLAsync(param);
                        }
                        break;
                    case Selection.Query:

                        response = await videoDownloader.ConvertFromVideoQueryAsync(param);
                        break;
                    case Selection.QueryFile:
                        if (File.Exists(param))
                        {
                            response = await videoDownloader.ConvertFromQueryFileAsync(param);
                        }
                        break;
                    case Selection.File:
                        if (File.Exists(param))
                        {
                            response = await videoDownloader.ConvertFromTextFileAsync(param);
                        }
                        break;
                }

                foreach (var item in response.ResponseMessages)
                {
                    switch (item.Status)
                    {
                        case DataEntities.StatusEnum.Success:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case DataEntities.StatusEnum.Warning:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case DataEntities.StatusEnum.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                    }

                    Console.WriteLine(item.ToString());
                }

                Console.ForegroundColor = ConsoleColor.White;
            }).Wait();

            if (args.Contains("--create-folders") || args.Contains("-cf"))
            {
                MusicBrainz _musicBrainz = new MusicBrainz(FolderPath);
                var response = _musicBrainz.CreateDirectoriesAfterPicardWasScanned();

                foreach (var item in response.ResponseMessages)
                {
                    switch (item.Status)
                    {
                        case DataEntities.StatusEnum.Success:
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;
                        case DataEntities.StatusEnum.Warning:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case DataEntities.StatusEnum.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                    }

                    Console.WriteLine(item.ToString());
                }
            }

            Console.ForegroundColor = ConsoleColor.White;
        }	

        enum Selection
        {
            Playlist,
            Video,
            File,
            Query,
            QueryFile,
            CreateFolders
        }

        //static void InteractiveMode()
        //{
        //    Selection? selection = null;

        //    Console.WriteLine("Youtube downloader extreme edition 😎");
        //    Console.WriteLine("------------------------");
        //    Console.WriteLine("");
        //    Console.WriteLine("Type a character for command");
        //    Console.WriteLine("");
        //    Console.WriteLine("------------------------");
        //    Console.WriteLine("");
        //    Console.WriteLine("P => Playlist");
        //    Console.WriteLine("V => Video");
        //    Console.WriteLine("F => File");
        //    Console.Write(">");

        //    while (selection == null)
        //    {
        //        var input = Console.ReadLine();

        //        switch (input.ToUpper())
        //        {
        //            case "P":
        //            case "PLAYLIST":
        //                selection = Selection.Playlist;
        //                break;
        //            case "V":
        //            case "VIDEO":
        //                selection = Selection.Video;
        //                break;
        //            case "F":
        //            case "FILE":
        //                selection = Selection.File;
        //                break;
        //            case "Q":
        //            case "QUERY":
        //                selection = Selection.Query;
        //                break;
        //            case "QF":
        //            case "QUERYFILE":
        //                selection = Selection.QueryFile;
        //                break;
        //            case "PI":
        //            case "PICARD":
        //                selection = Selection.Picard;
        //                break;
        //            default:
        //                Console.WriteLine("Wrong command. Please try again");
        //                Console.Write(">");
        //                break;
        //        }
        //    }

        //    Console.WriteLine($"You selected {selection.ToString()}");
        //    Console.Write($"Give me your link/file path :) >");

        //    var param = Console.ReadLine();

        //    YoutubeExplodeClient youtubeExplodeClient = new YoutubeExplodeClient(FolderPath);
        //    VideoDownloader videoDownloader = new VideoDownloader(FolderPath);
        //    Task.Run(async () =>
        //    {
        //        var response = new DataEntities.DataResponse<bool>();

        //        switch (selection)
        //        {
        //            case Selection.Playlist:
        //                if (Uri.IsWellFormedUriString(param, UriKind.Absolute))
        //                {
        //                    response = await videoDownloader.ConvertFromPlaylistURLAsync(param);
        //                }
        //                break;
        //            case Selection.Video:
        //                if (Uri.IsWellFormedUriString(param, UriKind.Absolute))
        //                {
        //                    response = await videoDownloader.ConvertFromVideoURLAsync(param);
        //                }
        //                break;
        //            case Selection.Query:

        //                response = await videoDownloader.ConvertFromVideoQueryAsync(param);
        //                break;
        //            case Selection.QueryFile:
        //                if (File.Exists(param))
        //                {
        //                    response = await videoDownloader.ConvertFromQueryFileAsync(param);
        //                }
        //                break;
        //            case Selection.File:
        //                if (File.Exists(param))
        //                {
        //                    response = await videoDownloader.ConvertFromTextFileAsync(param);
        //                }
        //                break;
        //            case Selection.Picard:
        //                break;
        //        }

        //        Console.WriteLine("Done :3");

        //        foreach (var item in response.ResponseMessages)
        //        {
        //            switch (item.Status)
        //            {
        //                case DataEntities.StatusEnum.Success:
        //                    Console.ForegroundColor = ConsoleColor.Green;
        //                    break;
        //                case DataEntities.StatusEnum.Warning:
        //                    Console.ForegroundColor = ConsoleColor.Yellow;
        //                    break;
        //                case DataEntities.StatusEnum.Error:
        //                    Console.ForegroundColor = ConsoleColor.Red;
        //                    break;
        //            }

        //            Console.WriteLine(item.ToString());
        //        }

        //        Console.ForegroundColor = ConsoleColor.White;
        //    }).Wait();

        //    Console.WriteLine("Press any key once you finished with Picard scanning");
        //    Console.ReadKey();

        //    MusicBrainz _musicBrainz = new MusicBrainz(FolderPath);
        //    var errors = _musicBrainz.CreateDirectoriesAfterPicardWasScanned();

        //    foreach (var errorModel in errors)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine(errorModel.ToString());
        //        Console.ForegroundColor = ConsoleColor.White;
        //    }

        //    Console.WriteLine("Done again :3");
        //    Console.ReadKey();
        //}
    }
}
