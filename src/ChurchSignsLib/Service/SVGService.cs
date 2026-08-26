using SkiaSharp;
using Svg.Skia;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace ChurchSignsLib.Service
{
    public class SVGService
    {

    public static async Task<BitmapImage>  RenderBitmapFromSVGString(string svgContent, int width = 800, int height = 600)
    {
            BitmapImage bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(svgContent)))
            {

                var svg = new SKSvg();
                if (svg.Load(stream) is null)
                    return null;

                using (SKBitmap bitmap = new SKBitmap(width, height))
                {
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        canvas.Clear(SKColors.White);

                        if (svg.Picture != null)
                        {
                            var bounds = svg.Picture.CullRect;
                            float scale = Math.Min(width / bounds.Width, height / bounds.Height);

                            canvas.Translate(
                                (width - bounds.Width * scale) / 2,
                                (height - bounds.Height * scale) / 2);
                            canvas.Scale(scale);

                            canvas.DrawPicture(svg.Picture);

                            
                        }
                    }
                    using (SKImage image = SKImage.FromBitmap(bitmap))
                    {
                        using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            using (MemoryStream ms = new MemoryStream(data.ToArray()))
                            {
                                ms.Position = 0;
                                await bitmapImage.StreamSource(ms.AsRandomAccessStream());
                            }
                        }
                    }

                }
            }
            

        }
}
}
