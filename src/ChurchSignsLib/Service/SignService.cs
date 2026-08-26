using ChurchSignsLib.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using Svg.Skia;

namespace ChurchSignsLib.Service
{
    public static class SignService
    {

        public static MergedSign GenerateTemplateImage(string svgContent)
        {
            MergedSign result = new MergedSign();
            result.Title = "Sparx Leader Template";
            
            using (var bitmap = SVGService.RenderBitmapFromSVGString(svgContent))
            {
                result.Bitmap = bitmap;
            }
            return result;
        }
    }
}
