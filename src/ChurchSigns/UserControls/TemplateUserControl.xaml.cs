using ChurchSigns.UI.Models;
using ChurchSigns.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Sys = Windows.System;


namespace ChurchSigns.UserControls
{
    public sealed partial class TemplateUserControl : UserControl
    {
        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(MainViewModel),
                typeof(TemplateUserControl),
                new PropertyMetadata(null, OnViewModelChanged));

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TemplateUserControl)d;

            if (e.OldValue is MainViewModel oldVm)
                oldVm.PropertyChanged -= control.ViewModel_PropertyChanged;

            if (e.NewValue is MainViewModel newVm)
            {
                newVm.PropertyChanged += control.ViewModel_PropertyChanged;
                control.RefreshGrouping();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(MainViewModel.GroupedTemplates))
            {
                RefreshGrouping();
            }
        }



        public SignTemplate? SelectedTemplate
        {
            get => (SignTemplate?)GetValue(SelectedTemplateProperty);
            set => SetValue(SelectedTemplateProperty, value);
        }

        public static readonly DependencyProperty SelectedTemplateProperty =
        DependencyProperty.Register(
            nameof(SelectedTemplate),
            typeof(SignTemplate),
            typeof(TemplateUserControl),
            new PropertyMetadata(null, OnSelectedTemplateChanged));

        private static void OnSelectedTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TemplateUserControl)d;
            var list = control.SignTemplatesListView;
            if (list is null) return;

            var value = e.NewValue as SignTemplate;
            if (!ReferenceEquals(list.SelectedItem, value))
                list.SelectedItem = value;
        }

        private void SignTemplatesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedTemplate = SignTemplatesListView.SelectedItem as SignTemplate;
            if (SelectedTemplate is null)
            {
                // the way the application is configured with provided templates
                // this should not happen. have provided templates been deleted?
                Debug.WriteLine("SignTemplatesListView changed selection to a null template!");
                return;
            }
            if (ViewModel is not null && !ReferenceEquals(ViewModel.SelectedTemplate, SelectedTemplate))
            {
                ViewModel.SelectedTemplate = SelectedTemplate;
            }
        }

        public TemplateUserControl()
        {
            InitializeComponent();
        }


        private async void DesignerInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://wpkramer.github.io/church-signs/TemplateDesigner.html");
            bool success = await Sys.Launcher.LaunchUriAsync(uri);
            if (!success)
            {
                if (ViewModel != null)
                {
                    await ViewModel.ShowMessageAsync("Unable to open Template Designer information at this time.");
                }
            }
        }


        private async void AddTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            bool importedTemplate = await ViewModel.ImportSignTemplateAsync();
        }



        private async void TemplateGrid_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel == null)
                return;
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        if (file.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = await ViewModel.ImportTemplateFileAsync(file);
                        }
                        else if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = await ViewModel.ImportTemplateFileAsync(file);
                        }
                        else
                        {
                            await ViewModel.ShowMessageAsync($"Rejected: {file.Name} (either a svg file or a zip file containing svg and json pairs)");
                        }
                    }
                }
            }
        }





        private void TemplateGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private void IsDesigner_Changed(object sender, RoutedEventArgs e)
        {
            if (ViewModel is null) return;
            ViewModel.IsDesignerMode = IsDesigner.IsChecked == true;
        }


        private async void ExportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel is null)
                return;
            await ViewModel.ExportSelectedTemplateAsync();
        }

        private async void RemoveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel is null)
                return;
            await ViewModel.RemoveSelectedTemplateAsync();
        }

        private void RefreshGrouping()
        {
            if (ViewModel?.GroupedTemplates is null)
                return;
            
            TemplatesCVS.Source = ViewModel.GroupedTemplates;
        }
    }
 
}
