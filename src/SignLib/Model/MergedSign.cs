using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SignLib.Model
{
    public class MergedSign
    {
        private SvgImageSource _imageSource;
        public MergedSign()
        {
            _imageSource = new SvgImageSource();
        }
        public string TemplatePath { get; set; }
        public string Title { get; set; }
        public Dictionary<string, string> MergeValues { get; set; } = new Dictionary<string, string>(); // e.g., { "{NAME}", "John" }
        public SvgImageSource Source { get { return _imageSource; } }
    }
}
