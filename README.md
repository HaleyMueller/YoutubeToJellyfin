# YoutubeToJellyfin
A small cross platform program that can convert youtube video links, playlists, or a text file to download youtube music. It embeds the video thumbnail and trims if needed. Saves the file structure how Jellyfin would want it.

## Prerequisites
- If you are running the contained release, you don't need to install .NET. If you aren't, you need .NET 8 (Runtime for running it, and/or SDK for running developing the project) [link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- FFMPEG runable from console [link](https://www.ffmpeg.org/download.html)
- (Optional) MusicBrainz Picard [link](https://picard.musicbrainz.org/)

## How to run
1. Launch the application by console
2. Pick your download type.
3. Wait for it to download.
4. (Optional) When it says press any key when you are done with picard, launch Picard and scan the newly downloaded songs. It will then find more metadata for your songs. You will then want to save the Picard changes. Back to the program you want to press any key to continue.
5. Continuing will move the songs to folders that Jellyfin likes: Arist/Album/Song
6. Copy the folder to your jellyfin Music folder

## MusicBrainz Picard
I recommend [Picard](https://picard.musicbrainz.org/) as this will put in additional meta data on your music that Jellyfin can pick up on easier.

## Development
Currently the only available working startup project is the `YoutubeToMusic.TestConsole` project
