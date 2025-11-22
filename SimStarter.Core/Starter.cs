using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimStarter.Core
{
    public sealed class SimApp
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool RunAsAdmin { get; set; }
        public bool WaitForExit { get; set; }
    }

    public sealed class AddonApp
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public bool RunAsAdmin { get; set; }
        public bool WaitForExit { get; set; }
    }

    public sealed class StarterProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string SimId { get; set; } = string.Empty;
        public List<string> AddonIds { get; set; } = new List<string>();
    }

    public sealed class ProfilesConfig
    {
        public List<SimApp> Sims { get; set; } = new List<SimApp>();
        public List<AddonApp> Addons { get; set; } = new List<AddonApp>();
        public List<StarterProfile> Starters { get; set; } = new List<StarterProfile>();
    }

    public static class PathUtil
    {
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var trimmed = path.Trim();
            if (trimmed.Length >= 2 && trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }
            return trimmed;
        }
    }

    public static class ProfilesStore
    {
        private const string ConfigFileName = "profiles.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string GetConfigPath()
        {
            var exeDir = AppContext.BaseDirectory;
            return Path.Combine(exeDir, ConfigFileName);
        }

        public static ProfilesConfig LoadOrCreate()
        {
            var path = GetConfigPath();

            if (!File.Exists(path))
            {
                var empty = new ProfilesConfig();
                Save(empty);
                return empty;
            }

            try
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<ProfilesConfig>(json, JsonOptions);
                return cfg ?? new ProfilesConfig();
            }
            catch
            {
                var empty = new ProfilesConfig();
                Save(empty);
                return empty;
            }
        }

        public static void Save(ProfilesConfig config)
        {
            var path = GetConfigPath();
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
        }
    }

    public static class StarterRunner
    {
        public static void RunStarter(ProfilesConfig config, StarterProfile starter, Action<string>? log = null)
        {
            var write = log ?? Console.WriteLine;
            write(string.Empty);
            write($"Starting: {starter.Name}");
            write(new string('-', 40));

            var sim = config.Sims.FirstOrDefault(s => s.Id == starter.SimId);
            if (sim == null)
            {
                write("[ERROR] No sim found for this starter.");
                return;
            }

            RunApp(sim.Name, sim.Path, sim.Arguments, sim.RunAsAdmin, sim.WaitForExit, write);

            var addonList = config.Addons.Where(a => starter.AddonIds.Contains(a.Id)).ToList();
            foreach (var addon in addonList)
            {
                RunApp(addon.Name, addon.Path, addon.Arguments, addon.RunAsAdmin, addon.WaitForExit, write);
            }

            write(string.Empty);
            write("All configured apps have been started (or attempted).");
        }

        private static void RunApp(string name, string path, string arguments, bool runAsAdmin, bool waitForExit, Action<string> log)
        {
            var normalizedPath = PathUtil.NormalizePath(path);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                log($"[SKIP] {name}: No path configured.");
                return;
            }

            if (!File.Exists(normalizedPath) && !LooksLikeExecutable(normalizedPath))
            {
                log($"[WARN] {name}: File not found: {normalizedPath}");
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var startInfo = new ProcessStartInfo
                {
                    FileName = normalizedPath,
                    Arguments = arguments ?? string.Empty,
                    UseShellExecute = true,
                    WorkingDirectory = ResolveWorkingDirectory(normalizedPath)
                };

                if (runAsAdmin)
                {
                    startInfo.Verb = "runas";
                }

                log($"[RUN] {name}: {startInfo.FileName} {startInfo.Arguments}");

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    log($"[ERROR] Failed to start: {name}");
                    return;
                }

                if (waitForExit)
                {
                    process.WaitForExit();
                    stopwatch.Stop();
                    log($"[DONE] {name} exited with code {process.ExitCode} in {stopwatch.Elapsed.TotalSeconds:F1}s.");
                }
                else
                {
                    // Quick health check: see if it dies immediately
                    var exitedQuickly = process.WaitForExit(2000);
                    stopwatch.Stop();
                    if (exitedQuickly)
                    {
                        log($"[WARN] {name} exited early with code {process.ExitCode} after {stopwatch.Elapsed.TotalSeconds:F1}s.");
                    }
                    else
                    {
                        log($"[OK] {name} started in {stopwatch.Elapsed.TotalSeconds:F1}s.");
                    }
                }
            }
            catch (Exception ex)
            {
                log($"[ERROR] {name}: {ex.Message}");
            }
        }

        private static string ResolveWorkingDirectory(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                return string.IsNullOrWhiteSpace(dir) ? Environment.CurrentDirectory : dir;
            }
            catch
            {
                return Environment.CurrentDirectory;
            }
        }

        private static bool LooksLikeExecutable(string path)
        {
            var ext = Path.GetExtension(path);
            return ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                   || ext.Equals(".bat", StringComparison.OrdinalIgnoreCase)
                   || ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase);
        }
    }
}
