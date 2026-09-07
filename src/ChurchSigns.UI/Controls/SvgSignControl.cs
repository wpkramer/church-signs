using ChurchSigns.UI.Models;
using ChurchSigns.UI.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChurchSigns.UI.Controls
{
    public partial class SvgSignControl : Control
    {
        private Image? _image;

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


        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _image = GetTemplateChild("PART_Image") as Image;
            _ = UpdateVisualAsync();
        }

        public Image? GetImage() { return _image; }

        private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgSignControl control)
                _ = control.UpdateVisualAsync();
        }


        private TemplatedImageSync _lastRenderProps = new TemplatedImageSync();
        private int _renderVersion = 0;


        private async Task UpdateVisualAsync()
        {
            // test if PART_Image has been applied
            if (_image is null)
                return;
            if (SvgTemplate is null)
                return;
            if(Data is null )
                return;

            TemplatedImageSync currentProps = new TemplatedImageSync(SvgTemplate, Data, RenderWidth, RenderHeight);

            if (currentProps.Template is null)
                return;
            if (currentProps.Template.Length == 0)
                return;


            if (currentProps.Equals(_lastRenderProps))
                return;

            var version = ++_renderVersion;



            try
            {
                var merged = currentProps.Template.MergeTemplateWithData(currentProps.Data);

                if (version != _renderVersion)
                    return; // superseded

                using (SKBitmap? skBitmap = merged.RenderToSKBitmap(currentProps.Width, currentProps.Height))
                {

                    if (skBitmap is null)
                    {
                        _image.Source = null;
                        return;
                    }

                    var bitmapImage = await skBitmap.ToBitmapImageAsync();

                    if (version != _renderVersion)
                        return;

                    _image.Source = bitmapImage;

                    _lastRenderProps.CopyFrom(currentProps);
                }


            }
            catch
            {
                // Optionally expose an Error state / logging
                if (_image is not null)
                    _image.Source = null;
            }

        }

    }
}
