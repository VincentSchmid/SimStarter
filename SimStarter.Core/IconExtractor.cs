using System;
using System.Drawing;
using System.IO;

namespace SimStarter.Core
{
    public static class IconExtractor
    {
        public static Icon? ExtractIcon(string? path)
        {
            var normalized = PathUtil.NormalizePath(path ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalized) || !File.Exists(normalized))
                return null;

            try
            {
                return Icon.ExtractAssociatedIcon(normalized);
            }
            catch
            {
                return null;
            }
        }
    }
}
