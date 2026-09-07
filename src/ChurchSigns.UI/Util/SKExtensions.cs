using ChurchSigns.UI.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Svg.Skia;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ChurchSigns.UI.Util
{
    public static class SKExtensions
    {
        // Make this extension public so callers can use SKBitmap.ToImageSourceAsync()
        public static async Task<SoftwareBitmapSource> ToImageSourceAsync(this SKBitmap skBitmap)
        {
            if (skBitmap == null)
                return null;

            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();

            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(softwareBitmap);
            return source;
        }

        public static SKBitmap? RenderToSKBitmap(this string svgContent, int width, int height)
        {
            if (string.IsNullOrWhiteSpace(svgContent) || width <= 0 || height <= 0)
                return null;

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
            var svg = new SKSvg();
            if (svg.Load(stream) is null || svg.Picture is null)
                return null;

            var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var bounds = svg.Picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return bitmap;

            float scale = Math.Min(width / bounds.Width, height / bounds.Height);
            float offsetX = (width - bounds.Width * scale) / 2f;
            float offsetY = (height - bounds.Height * scale) / 2f;

            canvas.Save();
            canvas.Translate(offsetX, offsetY);
            canvas.Scale(scale);
            canvas.Translate(-bounds.Left, -bounds.Top);
            canvas.DrawPicture(svg.Picture);
            canvas.Restore();

            return bitmap;
        }

        public static SKBitmap? RenderToSKBitmap(this string svgContent, PrintContentSize printSize)
        {
            ArgumentNullException.ThrowIfNull(printSize);
            return svgContent.RenderToSKBitmap(printSize.PixelWidth, printSize.PixelHeight);
        }

        public static async Task<BitmapImage> ToBitmapImageAsync(this SKBitmap skBitmap)
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var bitmapImage = new BitmapImage();
            using (var raStream = new InMemoryRandomAccessStream())
            {
                var bytes = data.ToArray();
                await raStream.WriteAsync(bytes.AsBuffer());
                raStream.Seek(0);
                await bitmapImage.SetSourceAsync(raStream);
            }
            return bitmapImage;
        }
    }
}
