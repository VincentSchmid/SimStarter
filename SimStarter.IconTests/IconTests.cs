using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using SimStarter.Core;
using SimStarter.UI;
using Xunit;

namespace SimStarter.IconTests
{
    public class IconTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Fact]
        public void ShortcutIconMatchesSimIconTopLeft()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
            var configPath = Path.Combine(repoRoot, "SimStarter.UI", "bin", "Debug", "net10.0-windows", "profiles.json");
            if (!File.Exists(configPath))
            {
                // Skip silently if config isn’t available.
                return;
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<ProfilesConfig>(json, JsonOptions) ?? new ProfilesConfig();
            Assert.NotEmpty(config.Starters);

            foreach (var starter in config.Starters)
            {
                var sim = config.Sims.FirstOrDefault(s => s.Id == starter.SimId);
                Assert.NotNull(sim);

                var simPath = PathUtil.NormalizePath(sim!.Path);
                if (!File.Exists(simPath))
                {
                    continue; // skip missing sims to avoid false failures on unavailable installs
                }

                var addons = config.Addons.Where(a => starter.AddonIds.Contains(a.Id)).ToList();
                var tempDir = Path.Combine(Path.GetTempPath(), "SimStarterTests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                var shortcutPath = Path.Combine(tempDir, $"{MakeSafeFileName(starter.Name)}.lnk");

                ShortcutService.CreateProfileShortcut(starter.Id, starter.Name, sim, addons, shortcutPath);

                var iconPath = GetShortcutIconPath(shortcutPath);
                Assert.True(File.Exists(iconPath), $"Icon path missing: {iconPath}");

                using var simBmp = Icon.ExtractAssociatedIcon(simPath)?.ToBitmap();
                using var shortcutBmp = Icon.ExtractAssociatedIcon(iconPath)?.ToBitmap();

                Assert.NotNull(simBmp);
                Assert.NotNull(shortcutBmp);

                Assert.True(RegionMatches(simBmp!, shortcutBmp!, 24), $"Icon mismatch for starter '{starter.Name}'");
            }
        }

        private static string GetShortcutIconPath(string shortcutPath)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                            ?? throw new InvalidOperationException("WScript.Shell is not available.");
            dynamic shell = Activator.CreateInstance(shellType)
                                ?? throw new InvalidOperationException("Failed to create WScript.Shell.");
            dynamic link = shell.CreateShortcut(shortcutPath);
            string iconLocation = link.IconLocation;
            if (string.IsNullOrWhiteSpace(iconLocation)) return string.Empty;

            var parts = iconLocation.Split(',');
            var path = parts[0].Trim();
            return path;
        }

        private static bool RegionMatches(Bitmap a, Bitmap b, int regionSize)
        {
            var size = Math.Min(regionSize, Math.Min(a.Width, Math.Min(a.Height, Math.Min(b.Width, b.Height))));

            using var aScaled = new Bitmap(size, size);
            using (var g = Graphics.FromImage(aScaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(a, new Rectangle(0, 0, size, size));
            }

            using var bScaled = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bScaled))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(b, new Rectangle(0, 0, size, size));
            }

            var c1 = aScaled.GetPixel(0, 0);
            var c2 = bScaled.GetPixel(0, 0);
            int channelTolerance = 64;
            return Math.Abs(c1.R - c2.R) <= channelTolerance
                   && Math.Abs(c1.G - c2.G) <= channelTolerance
                   && Math.Abs(c1.B - c2.B) <= channelTolerance
                   && Math.Abs(c1.A - c2.A) <= channelTolerance;
        }

        private static string MakeSafeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                name = name.Replace(c, '_');
            }
            return string.IsNullOrWhiteSpace(name) ? "Starter" : name;
        }
    }
}
