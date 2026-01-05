# YoutubeToJellyfin
A small cross platform program that can convert youtube video links, playlists, or a text file to download youtube music. It embeds the video thumbnail and song metadata from MusicBrainz. Then saves the file structure in the format for Jellyfin to read.

## Features
- Metadata for Title, Artist, Album art.
- Fingerprinting for MusicBrainz song matching.
- Auto detection and correcting if the video thumbnail is in the "Square image but imma make it a rectangle for ya by doing a solid color on the border"
- Auto moves files into the Jellyfin folder format

## Prerequisites
- If you are running the contained release, you don't need to install .NET. If you aren't, you need .NET 8 (Runtime for running it, and/or SDK for running developing the project) [link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- FFMPEG runable from console [link](https://www.ffmpeg.org/download.html)

## Usage
-  -p  "playlistUrl"  (playlist)
-  -v  "videoUrl"     (video)
-  -q  "search query" (query)
-  -qf queryFile.txt  (file of queries)
-  -f  urls.txt       (file of urls)
-  -cf                (create folders for Jellyfin)

## How To Build
- dotnet build `YoutubeToMusic.TestConsole`

## Development
Currently the only available working startup project is the `YoutubeToMusic.TestConsole` project
