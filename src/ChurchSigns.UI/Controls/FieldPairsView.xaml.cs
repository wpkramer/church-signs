using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChurchSigns.UI.Controls
{
    public sealed partial class FieldPairsView : UserControl
    {
        private readonly Dictionary<string, string> _fields;

        public delegate void SingleSignDataHandler(IReadOnlyDictionary<string, string> data);
        public event SingleSignDataHandler? SingleSignDataChanged;

        public FieldPairsView()
        {
            _fields = new Dictionary<string, string>();
            InitializeComponent();
        }

        public static readonly DependencyProperty SelectedTemplateProperty =
            DependencyProperty.Register(
            nameof(SelectedTemplate),
            typeof(SignTemplate),
            typeof(FieldPairsView),
            new PropertyMetadata(null, OnTemplateChanged));

        public static readonly DependencyProperty PastedDataProperty =
            DependencyProperty.Register(
            nameof(PastedData),
            typeof(PastedFieldPairs),
            typeof(FieldPairsView),
            new PropertyMetadata(new PastedFieldPairs(string.Empty), OnPasteDataChanged));


        private static void OnPasteDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FieldPairsView control)
            {
                control.Rebuild();
            }
        }

        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FieldPairsView control)
            {
                control.Rebuild();
            }
        }

        public PastedFieldPairs PastedData
        {
            get => (PastedFieldPairs)GetValue(PastedDataProperty);
            set => SetValue(PastedDataProperty, value);
        }

        public SignTemplate SelectedTemplate
        {
            get => (SignTemplate)GetValue(SelectedTemplateProperty);
            set => SetValue(SelectedTemplateProperty, value);
        }

        public IReadOnlyDictionary<string, string> Fields => _fields;

        private void Rebuild()
        {
            if(SingleSignDataChanged != null)
            {
                SingleSignDataChanged(Fields);
            }
        }
    }
}
