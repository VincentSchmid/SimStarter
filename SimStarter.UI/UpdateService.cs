using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimStarter.UI
{
    internal static class UpdateService
    {
        private static readonly HttpClient Http = new HttpClient();

        public enum UpdateResult
        {
            NoUpdate,
            UpdatingAndRestarting,
            Failed
        }

        public static async Task<UpdateResult> CheckForUpdatesAsync(string owner, string repo, Version currentVersion, Action<string> log)
        {
            try
            {
                var latest = await FetchLatestReleaseAsync(owner, repo);
                if (latest == null)
                {
                    log("No release info found.");
                    return UpdateResult.Failed;
                }

                if (!Version.TryParse(latest.Tag?.TrimStart('v', 'V'), out var latestVersion))
                {
                    log($"Could not parse release version '{latest.Tag}'.");
                    return UpdateResult.Failed;
                }

                log($"Current version: {currentVersion}, Latest: {latestVersion}");
                if (latestVersion <= currentVersion)
                {
                    log("Already on the latest version.");
                    return UpdateResult.NoUpdate;
                }

                var asset = latest.GetAssetForPlatform("SimStarter.UI", ".zip");
                if (asset == null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) || string.IsNullOrWhiteSpace(asset.Name))
                {
                    log("No suitable release asset found.");
                    return UpdateResult.Failed;
                }

                log($"Downloading {asset.Name} ...");
                var tempRoot = Path.Combine(Path.GetTempPath(), "SimStarterUpdate", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempRoot);
                var zipPath = Path.Combine(tempRoot, asset.Name);

                using (var resp = await Http.GetAsync(asset.BrowserDownloadUrl))
                {
                    resp.EnsureSuccessStatusCode();
                    await using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write);
                    await resp.Content.CopyToAsync(fs);
                }

                var extractDir = Path.Combine(tempRoot, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);
                log("Download complete. Preparing update...");

                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exePath))
                {
                    log("Cannot determine current executable path.");
                    return UpdateResult.Failed;
                }

                var scriptPath = Path.Combine(tempRoot, "update.cmd");
                var exeName = Path.GetFileName(exePath);
                var destDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
                var script = CreateUpdateScript(extractDir, destDir, exeName, Process.GetCurrentProcess().Id);
                File.WriteAllText(scriptPath, script);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{scriptPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                log("Updater started. The app will close and restart after update.");
                return UpdateResult.UpdatingAndRestarting;
            }
            catch (Exception ex)
            {
                log($"Update failed: {ex.Message}");
                return UpdateResult.Failed;
            }
        }

        private static string CreateUpdateScript(string sourceDir, string destDir, string exeName, int currentPid)
        {
            return $@"
@echo off
setlocal
set SRC=""{sourceDir}""
set DST=""{destDir}""
:wait
tasklist /FI ""PID eq {currentPid}"" | find ""{currentPid}"" >nul
if %ERRORLEVEL%==0 (
  timeout /t 1 >nul
  goto wait
)
robocopy %SRC% %DST% /MIR >nul
start """" ""%DST%\{exeName}""
endlocal
";
        }

        private static async Task<ReleaseInfo?> FetchLatestReleaseAsync(string owner, string repo)
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("SimStarter-Updater");
            var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReleaseInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private sealed class ReleaseInfo
        {
            public string? Tag { get; set; }
            public string? Name { get; set; }
            public ReleaseAsset[] Assets { get; set; } = Array.Empty<ReleaseAsset>();

            public ReleaseAsset? GetAssetForPlatform(string prefix, string suffix)
            {
                foreach (var asset in Assets)
                {
                    if (asset.Name != null &&
                        asset.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                        asset.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        return asset;
                    }
                }
                return null;
            }
        }

        private sealed class ReleaseAsset
        {
            public string? Name { get; set; }
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}
