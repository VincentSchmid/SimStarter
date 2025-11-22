using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimStarter.Core;

namespace SimStarter.UI
{
    internal static class IconHelper
    {
        public static ImageSource? GetIcon(string? path)
        {
            var normalized = PathUtil.NormalizePath(path ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalized) || !File.Exists(normalized))
                return null;

            try
            {
                using var icon = Icon.ExtractAssociatedIcon(normalized);
                if (icon == null) return null;

                var img = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                img.Freeze();
                return img;
            }
            catch
            {
                return null;
            }
        }

    }
}
