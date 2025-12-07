using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SimStarter.Core
{
    public static class ShortcutService
    {
        public static string CreateProfileShortcut(string profileId, string profileName, SimApp sim, IEnumerable<AddonApp> addonsForProfile, string shortcutPath)
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                          ?? throw new InvalidOperationException("Cannot resolve current executable path.");

            var simIconPath = PathUtil.NormalizePath(sim.Path);
            var iconPath = BuildIcon(profileId, sim, addonsForProfile);

            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                            ?? throw new InvalidOperationException("WScript.Shell is not available.");
            dynamic shell = Activator.CreateInstance(shellType)
                                ?? throw new InvalidOperationException("Failed to create WScript.Shell.");
            dynamic link = shell.CreateShortcut(shortcutPath);
            link.TargetPath = exePath;
            link.Arguments = $"--run-profile-id=\"{profileId}\"";
            link.WorkingDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
            // Prefer composed icon; if we fail to build one, fall back to the sim's own icon,
            // otherwise the shortcut would show the SimStarter icon.
            link.IconLocation = !string.IsNullOrWhiteSpace(iconPath) ? iconPath
                                 : !string.IsNullOrWhiteSpace(simIconPath) && File.Exists(simIconPath) ? simIconPath
                                 : exePath;
            link.Description = $"Start SimStarter profile '{profileName}'";
            link.Save();

            return shortcutPath;
        }

        private static string? BuildIcon(string profileId, SimApp sim, IEnumerable<AddonApp> addons)
        {
            var addonList = addons.Take(3).ToList();

            var simIconPath = PathUtil.NormalizePath(sim.Path);
            if (string.IsNullOrWhiteSpace(simIconPath) || !File.Exists(simIconPath)) return null;

            try
            {
                using var baseIcon = Icon.ExtractAssociatedIcon(simIconPath);
                if (baseIcon == null) return null;

                var size = 64;
                using var canvas = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(canvas))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                    using var baseBmp = baseIcon.ToBitmap();
                    g.DrawImage(baseBmp, new Rectangle(0, 0, size, size));

                    var overlaySize = 18;
                    var margin = 4;
                    for (var i = 0; i < addonList.Count; i++)
                    {
                        var addonPath = PathUtil.NormalizePath(addonList[i].Path);
                        if (string.IsNullOrWhiteSpace(addonPath) || !File.Exists(addonPath)) continue;

                        using var addonIcon = Icon.ExtractAssociatedIcon(addonPath);
                        if (addonIcon == null) continue;
                        using var addonBmp = addonIcon.ToBitmap();

                        var x = size - overlaySize - margin;
                        var y = size - overlaySize - margin - (i * (overlaySize + 2));
                        g.DrawImage(addonBmp, new Rectangle(x, y, overlaySize, overlaySize));
                    }
                }

                var iconsDir = Path.Combine(AppContext.BaseDirectory, "profile-icons");
                Directory.CreateDirectory(iconsDir);
                var iconFile = Path.Combine(iconsDir, $"{profileId}.ico");
                using var fs = new FileStream(iconFile, FileMode.Create, FileAccess.Write);
                using var iconFromHandle = Icon.FromHandle(canvas.GetHicon());
                iconFromHandle.Save(fs);
                return iconFile;
            }
            catch
            {
                return null;
            }
        }
    }
}
