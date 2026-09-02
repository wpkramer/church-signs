using ChurchSigns.UI.Models;
using ChurchSigns.UI.ViewModels;
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

namespace ChurchSigns.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SignTemplateDialog : ContentDialog
    {
        public SignTemplateViewModel ViewModel { get; private set; }
        private readonly SignTemplate _signTemplate;
        public SignTemplateDialog(SignTemplate signTemplate)
        {
            ArgumentNullException.ThrowIfNull(signTemplate, nameof(signTemplate));
            _signTemplate = signTemplate;
            ViewModel = new SignTemplateViewModel(_signTemplate);
            //foreach (var dictItem in _signTemplate.PreviewFields)
            //{
            //    ViewModel.PreviewFields[dictItem.Key] = dictItem.Value;
            //}
            InitializeComponent();
        }
    }
}
