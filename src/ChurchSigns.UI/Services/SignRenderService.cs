using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Util;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChurchSigns.UI.Services
{

    public static class SignRenderService
    {

        //public static SKBitmap RenderPrintSizeBitmap(ChurchSign sign)
        //{

        //    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sign.SvgTemplate.MergeTemplateWithData(sign.Fields)));
        //    var svg = new SKSvg();

        //    if (svg.Load(stream) is null || svg.Picture is null)
        //        return null;

        //    var bitmap = new SKBitmap((int)sign.PrintSize.ContentWidthPt, (int)sign.PrintSize.ContentHeightPt);
        //    using var canvas = new SKCanvas(bitmap);
        //    canvas.Clear(SKColors.White);

        //    var bounds = svg.Picture.CullRect;
        //    if (bounds.Width <= 0 || bounds.Height <= 0)
        //        return bitmap;

        //    float scale = Math.Min(sign.PrintSize.ContentWidthPt / bounds.Width, sign.PrintSize.ContentHeightPt / bounds.Height);
        //    float offsetX = (sign.PrintSize.ContentWidthPt - bounds.Width * scale) / 2f;
        //    float offsetY = (sign.PrintSize.ContentHeightPt - bounds.Height * scale) / 2f;

        //    canvas.Translate(offsetX, offsetY);
        //    canvas.Scale(scale);
        //    canvas.DrawPicture(svg.Picture);

        //    return bitmap;
        //}

        private static async Task<SoftwareBitmapSource> ToImageSourceAsync(SKBitmap skBitmap)
        {
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
    }
}

// might bring back for direct printing, keep in comments just for future reference


//public static async Task<ImageSource> RenderToImageSourceAsync(string svgTemplate, Dictionary<string, string> fields, int pixelWidth, int pixelHeight)
//{
//    ImageSource source = null;
//    try
//    {

//        var merged = svgTemplate.MergeTemplateWithData(fields);

//        using (SKBitmap bitmap = RenderPrintSizeBitmap(merged, pixelWidth, pixelHeight))
//        {
//            if (bitmap is null)
//            {
//                return null;
//            }

//            source = await ToImageSourceAsync(bitmap);
//        }
//    }
//    catch (Exception ex)
//    {
//        // Log the exception or handle it as needed
//        System.Diagnostics.Trace.WriteLine($"Error rendering sign: {ex.Message}");
//        return null;

//    }
//    return source;
//}

//public static async Task<Image> RenderToImageAsync(string svgTemplate, Dictionary<string, string> fields, int pixelWidth, int pixelHeight, double pageWidthDips, double pageHeightDips)
//{
//    try
//    {

//        var merged = svgTemplate.MergeTemplateWithData(fields);

//        using (SKBitmap bitmap = RenderPrintSizeBitmap(merged, pixelWidth, pixelHeight))
//        {
//            if (bitmap is null)
//            {
//                return null;
//            }

//            var source = await ToImageSourceAsync(bitmap);
//            var image = new Image
//            {
//                Source = source,
//                Width = pageWidthDips,
//                Height = pageHeightDips,
//                Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform
//            };
//            return image;
//        }
//    }
//    catch (Exception ex)
//    {
//        // Log the exception or handle it as needed
//        System.Diagnostics.Trace.WriteLine($"Error rendering sign: {ex.Message}");
//        return null;

//    }



