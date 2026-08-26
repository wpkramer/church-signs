using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChurchSignsLib.Model
{
    public class MergedSign
    {
        public string Title { get; set; }
        public string ImagePath { get; set; }
        public SKBitmap Bitmap { get; internal set; }
    }
}
