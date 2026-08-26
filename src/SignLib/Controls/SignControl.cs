using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SignLib.Controls
{
    /// <summary>
    /// A WebView2 wrapper that generates HTML from a
    /// SVG file replacing fields using a Dictionary
    /// that maps a field name to the replacement value
    /// </summary>
    /// <remarks>
    /// The XAML Image control provides minimal set of
    /// SVG functionality as it is meant to support icons
    /// and such. SKSvg + SkiaSharp offer rich functionality
    /// but it is a load of libraries. WebView2 brings in the
    /// full SVG compatibility for what I want to render.
    /// </remarks>
    public partial class SignControl : Control
    {

        private WebView2? _webView = null;

        public SignControl()
        {
            DefaultStyleKey = typeof(SignControl);
        }

        public static readonly DependencyProperty SvgTemplateProperty =
    DependencyProperty.Register(
        nameof(SvgTemplate),
        typeof(string),
        typeof(SignControl),
        new PropertyMetadata(string.Empty));
        
        /// <summary>
        /// DependencyProperty for the template to use for the
        /// SVG image to display
        /// </summary>
        public string SvgTemplate
        {
            get => (string)GetValue(SvgTemplateProperty);
            set => SetValue(SvgTemplateProperty, value);
        }

        
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(Dictionary<string, string>),
                typeof(SignControl),
                new PropertyMetadata(null, OnDataChanged)
            );

        // Property getter/setter
        public Dictionary<string, string> Data
        {
            get => (Dictionary<string, string>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        // Optional: callback when the property changes
        private static void OnDataChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            // Handle changes here if needed
        }
    }
}
