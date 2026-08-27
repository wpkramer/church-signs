using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
#nullable enable
namespace ChurchSigns.UI.Controls
{
    public partial class SvgSignControl : Control
    {
        private Image? _image;
        private bool _templatePropertyChanged = false;
        private bool _dataPropertyChanged = false;
        public SvgSignControl()
        {
            DefaultStyleKey = typeof(SvgSignControl);
        }

        // ─── SvgTemplate ───────────────────────────────────────────────

        public static readonly DependencyProperty SvgTemplateProperty =
            DependencyProperty.Register(
                nameof(SvgTemplate),
                typeof(string),
                typeof(SvgSignControl),
                new PropertyMetadata(null, OnRenderPropertyChanged));

        public string? SvgTemplate
        {
            get => (string?)GetValue(SvgTemplateProperty);
            set => SetValue(SvgTemplateProperty, value);
        }

        // ─── Data (field mapping) ──────────────────────────────────────

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(IDictionary<string, string>),
                typeof(SvgSignControl),
                new PropertyMetadata(null, OnRenderPropertyChanged));


        public IDictionary<string, string>? Data
        {
            get => (IDictionary<string, string>?)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        // ─── render size (logical pixels) ────────────────────

        public static readonly DependencyProperty RenderWidthProperty =
            DependencyProperty.Register(
                nameof(RenderWidth),
                typeof(int),
                typeof(SvgSignControl),
                new PropertyMetadata(800, OnRenderPropertyChanged));

        public int RenderWidth
        {
            get => (int)GetValue(RenderWidthProperty);
            set => SetValue(RenderWidthProperty, value);
        }

        public static readonly DependencyProperty RenderHeightProperty =
            DependencyProperty.Register(
                nameof(RenderHeight),
                typeof(int),
                typeof(SvgSignControl),
                new PropertyMetadata(600, OnRenderPropertyChanged));

        public int RenderHeight
        {
            get => (int)GetValue(RenderHeightProperty);
            set => SetValue(RenderHeightProperty, value);
        }

        // ─── Template + lifecycle ──────────────────────────────────────

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _image = GetTemplateChild("PART_Image") as Image;
            _ = UpdateVisualAsync();
        }

        private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgSignControl control)
                _ = control.UpdateVisualAsync();
        }

        private class SynchData
        {
            private string? _template;
            private IDictionary<string, string>? _data;
            private int _width;
            private int _height;
            private object _dataLock = new object();

            public SynchData()
            {
                _template = null;
                _data = null;
                _width = 0;
                _height = 0;
            }

            //public SynchData(SynchData props)
            //{
            //    _template = props.Template;
            //    _data = props.Data;
            //    _width = props.Width;
            //    _height = props.Height;
            //}

            public SynchData(string? template, IDictionary<string, string>? data, int width, int height)
            {
                _template = template;
                _data = data;
                _width = width;
                _height = height;
            }

            public void CopyFrom(SynchData other)
            {
                if (other == null)
                    throw new ArgumentNullException(nameof(other));

                _template = other._template;
                _data = other._data;
                _width = other._width;
                _height = other._height;
            }

            public string? Template { get => _template; }
            public IDictionary<string,string>? Data { get => _data; }
            public int Width { get => _width; }
            public int Height { get => _height; }

            public bool Equals(SynchData other)
            {
                if (other is null) return false;
                if(ReferenceEquals(this, other)) return true;
                if(_width != other._width) return false;
                if(_height != other._height) return false;
                if(_template != other._template) return false;
                if (_data == null && other._data != null) return false;
                if(_data != null && other._data == null) return false;
                if(_data != null && other._data != null)
                {
                    if(_data.Count != other._data.Count) return false;
                }
                return ReferenceEquals(_data, other._data);
            }
        }
        private SynchData _lastRenderProps = new SynchData();
        private int _renderVersion = 0;


        private async Task UpdateVisualAsync()
        {
            // test if PART_Image has been applied
            if (_image is null)
                return;

            SynchData currentProps = new SynchData(SvgTemplate, Data, RenderWidth, RenderHeight);

            if (currentProps.Template is null)
                return;

            //var template = SvgTemplate;
            //if (template is null)
            //    return;
            //var data = Data;
            //var width = RenderWidth;
            //var height = RenderHeight;

            if (currentProps.Equals(_lastRenderProps))
                return;

            var version = ++_renderVersion;


            SKBitmap? bitmap = null;
            try
            {
                var merged = Merge(currentProps.Template, currentProps.Data);

                if (version != _renderVersion)
                    return; // superseded

                bitmap = RenderToBitmap(merged, currentProps.Width, currentProps.Height);

                if (bitmap is null)
                {
                    _image.Source = null;
                    return;
                }

                var source = await ToImageSourceAsync(bitmap);

                if (version != _renderVersion)
                    return;

                _image.Source = source;

                _lastRenderProps.CopyFrom(currentProps);
                
                //_lastTemplate = template;
                //_lastData = data;
                //_lastWidth = width;
                //_lastHeight = height;

            }
            catch
            {
                // Optionally expose an Error state / logging
                if (_image is not null)
                    _image.Source = null;
            }
            finally
            {
                bitmap?.Dispose();

            }
        }

        // ─── Merge {{Field}} placeholders ──────────────────────────────
        // TODO: move Merge logic to a string extension
        private static string Merge(string template, IDictionary<string, string>? data)
        {
            if (data is null || data.Count == 0)
                return template;

            return FieldReplaceRegex().Replace(template, m =>
            {
                var key = m.Groups[1].Value;
                return data.TryGetValue(key, out var value) ? value : m.Value;
            });
        }

        // ─── SkiaSharp + Svg.Skia render ───────────────────────────────

        private static SKBitmap? RenderToBitmap(string svgContent, int width, int height)
        {
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

            canvas.Translate(offsetX, offsetY);
            canvas.Scale(scale);
            canvas.DrawPicture(svg.Picture);

            return bitmap;
        }

        // ─── SKBitmap → WinUI ImageSource ──────────────────────────────

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

        [GeneratedRegex(@"\{\{\s*(.+?)\s*\}\}")]
        private static partial Regex FieldReplaceRegex();
    }
}
