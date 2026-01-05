using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YoutubeToMusic.BLL
{
    public class Fingerprinting
    {
        private string ApiKey;
        public Fingerprinting(string acoustAPIKey)
        {
            ApiKey = acoustAPIKey;
        }

        public async Task<DataEntities.DataResponse<DataEntities.FingerprintLookup>> Fingerprint(string path)
        {
            var ret = new DataEntities.DataResponse<DataEntities.FingerprintLookup>();

            try
            {
                var fingerprint = FingerprintAudio(path);

                var a = await LookupAsync(fingerprint.fingerprint, fingerprint.duration, ApiKey);

                ret.Data = a;
            }
            catch (Exception ex)
            {
                ret.AddException(ex);
                return ret;
            }

            return ret;
        }

        public static async Task<DataEntities.FingerprintLookup> LookupAsync(string fingerprint, int duration, string apiKey)
        {
            if (string.IsNullOrEmpty(fingerprint))
                throw new ArgumentException("Fingerprint is required", nameof(fingerprint));

            if (duration <= 0)
                throw new ArgumentException("Duration must be positive", nameof(duration));

            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required", nameof(apiKey));

            // Build the query URL
            var url = $"https://api.acoustid.org/v2/lookup?client={apiKey}" +
                      $"&duration={duration}" +
                      $"&fingerprint={Uri.EscapeDataString(fingerprint)}" +
                      $"&meta=recordings+releases+artists+recordingids" +
                      $"&format=json";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<DataEntities.FingerprintLookup>(json);
            }
        }

        public static (string fingerprint, int duration) FingerprintAudio(string filePath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetFpcalcPath(),
                Arguments = $"\"{filePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string fingerprint = null;
            int duration = 0;

            foreach (var line in output.Split('\n'))
            {
                if (line.StartsWith("FINGERPRINT="))
                    fingerprint = line.Replace("FINGERPRINT=", "").Trim();

                if (line.StartsWith("DURATION="))
                    duration = int.Parse(line.Replace("DURATION=", "").Trim());
            }

            return (fingerprint, duration);
        }

        static string GetFpcalcPath()
        {
            string baseDir = Path.Combine(
                AppContext.BaseDirectory,
                "Programs",
                "chromaprint"
            );

            string rid;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                rid = "windows";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                rid = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "linux-x64",
                    Architecture.Arm64 => "linux-arm64",
                    _ => throw new PlatformNotSupportedException("Unsupported Linux architecture")
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                rid = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X64 => "osx-x64",
                    Architecture.Arm64 => "osx-arm64",
                    _ => throw new PlatformNotSupportedException("Unsupported macOS architecture")
                };
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS");
            }

            string exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "fpcalc.exe"
                : "fpcalc";

            return Path.Combine(baseDir, rid, exeName);
        }
    }
}
