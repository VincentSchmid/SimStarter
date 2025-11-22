using System;
using System.IO;
using System.Reflection;

namespace SimStarter.UI
{
    internal static class VersionProvider
    {
        public static string GetVersionString()
        {
            var fileVersion = ReadVersionFile();
            if (!string.IsNullOrWhiteSpace(fileVersion))
            {
                return fileVersion!;
            }

            var asm = Assembly.GetExecutingAssembly();
            var v = asm.GetName().Version;
            return v != null ? v.ToString() : "0.0.0";
        }

        private static string? ReadVersionFile()
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "VERSION");
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
