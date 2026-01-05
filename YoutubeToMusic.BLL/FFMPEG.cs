using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeToMusic.BLL
{
    public class FFMPEG
    {
        public static async Task<string> ConvertFile(string inputFilePath, string outputFilePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" \"{outputFilePath}\" -y",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                process.Exited += Process_Exited;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.Disposed += Process_Disposed;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                await process.WaitForExitAsync();
            }

            File.Delete(inputFilePath);

            return File.Exists(outputFilePath) ? $"Conversion successful: {outputFilePath}" : "Conversion failed.";
            Console.WriteLine(File.Exists(outputFilePath) ? $"Conversion successful: {outputFilePath}" : "Conversion failed.");
        }

        public static void CombineAudioVideo(string videoFilePath, string audioFilePath, string outputFilePath)
        {
            
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{videoFilePath}\" -i \"{audioFilePath}\" -c:v copy -map 0:v:0 -map 1:a:0 -shortest \"{outputFilePath}\" -y",
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                process.Exited += Process_Exited;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.Disposed += Process_Disposed;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.WaitForExit();
            }

            Console.WriteLine(File.Exists(outputFilePath)
                ? $"Muxing successful: {outputFilePath}"
                : "Muxing failed.");
        }


        /// <summary>
        /// Create a image in a square format that will clip the edges of the image into a square aspect ratio
        /// </summary>
        public static void ConvertToSquarePngByTrimmingBorders(string inputFilePath, string outputFilePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" -vf \"crop='min(iw,ih)':'min(iw,ih)'\" \"{outputFilePath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                process.Exited += Process_Exited;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.Disposed += Process_Disposed;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.WaitForExit();
            }

            File.Delete(inputFilePath);

            Console.WriteLine(File.Exists(outputFilePath) ? $"Conversion successful: {outputFilePath}" : "Conversion failed.");
        }

        /// <summary>
        /// Create a image in a square format that will expand/shrink the aspect ratio into it
        /// </summary>
        public static void ConvertToSqaurePngByShrinking(string inputFilePath, string outputFilePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" -vf \"scale='if(gt(iw,ih),ih,iw)':'if(gt(iw,ih),ih,iw)',pad='if(gt(iw,ih),iw,ih)':'if(gt(iw,ih),iw,ih)':(ow-iw)/2:(oh-ih)/2\" \"{outputFilePath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();
                process.Exited += Process_Exited;
                process.OutputDataReceived += Process_OutputDataReceived;
                process.Disposed += Process_Disposed;
                process.ErrorDataReceived += Process_ErrorDataReceived;
                process.WaitForExit();
            }

            File.Delete(inputFilePath);

            Console.WriteLine(File.Exists(outputFilePath) ? $"Conversion successful: {outputFilePath}" : "Conversion failed.");
        }

        public static double GetDuration(string filePath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            double seconds = double.Parse(output, CultureInfo.InvariantCulture);

            return seconds;
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static void Process_Disposed(object? sender, EventArgs e)
        {
            Console.WriteLine(e);
        }

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private static void Process_Exited(object? sender, EventArgs e)
        {
            Console.WriteLine(e);
        }
    }
}
