using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace SimStarter.Core
{
    public static class VersionProvider
    {
        public static string GetVersionString()
        {
            var fileVersion = ReadVersionFile();
            if (!string.IsNullOrWhiteSpace(fileVersion)) return NormalizeVersionString(fileVersion!);

            var asm = Assembly.GetExecutingAssembly();
            var infoAttr = asm.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoAttr)) return NormalizeVersionString(infoAttr!);

            var v = asm.GetName().Version;
            return v != null ? NormalizeVersionString(v.ToString()) : "0.0.0";

        }

        private static string? ReadVersionFile()
        {
            return TryReadVersionFile(AppContext.BaseDirectory)
                   ?? TryReadVersionFile(Environment.CurrentDirectory)
                   ?? TryReadVersionFile(SearchUpwardsForVersion(Environment.CurrentDirectory));
        }

        private static string NormalizeVersionString(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "0.0.0";
            var trimmed = v.Trim();
            // Strip leading refs/tags/, v, V
            if (trimmed.StartsWith("refs/tags/", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring("refs/tags/".Length);
            }
            if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(1);
            }

            // Drop build metadata after '+'
            var plusIdx = trimmed.IndexOf('+');
            if (plusIdx >= 0) trimmed = trimmed.Substring(0, plusIdx);

            // Drop suffix after space
            var spaceIdx = trimmed.IndexOf(' ');
            if (spaceIdx >= 0) trimmed = trimmed.Substring(0, spaceIdx);

            // Keep numeric/period only at start
            return trimmed;
        }

        private static string? TryReadVersionFile(string? baseDir)
        {
            if (string.IsNullOrWhiteSpace(baseDir)) return null;
            try
            {
                var path = Path.Combine(baseDir, "VERSION");
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? SearchUpwardsForVersion(string startDir)
        {
            try
            {
                var dir = new DirectoryInfo(startDir);
                while (dir != null)
                {
                    var candidate = Path.Combine(dir.FullName, "VERSION");
                    if (File.Exists(candidate))
                    {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }
            }
            catch
            {
                // ignore
            }
            return null;
        }
    }
}
