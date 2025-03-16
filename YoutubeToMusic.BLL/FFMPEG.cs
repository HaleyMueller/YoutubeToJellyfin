using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeToMusic.BLL
{
    public class FFMPEG
    {
        public static void ConvertFile(string inputFilePath, string outputFilePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" \"{outputFilePath}\" -y",
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
