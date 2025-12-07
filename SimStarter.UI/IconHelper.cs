using System;
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
            try
            {
                using var icon = IconExtractor.ExtractIcon(path);
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
