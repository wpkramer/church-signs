using ChurchSigns.UI.Controls;
using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using ChurchSigns.UI.Util;
using ChurchSigns.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Sys = Windows.System;

namespace ChurchSigns
{
    /// <summary>
    /// Church Signs is a one Window / Page Appliction
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public MainViewModel ViewModel { get; } = new();

        public MainWindow()
        {
            InitializeComponent();


            MappingView.MultiSignDataChanged += MappingView_MappedDataChanged;
            PairsView.SingleSignDataChanged += PairsView_SingleSignDataChanged;


            Clipboard.ContentChanged += Clipboard_ContentChanged;
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);


            this.Activated += MainWindow_Activated;

        }

        private void PairsView_SingleSignDataChanged(IReadOnlyDictionary<string, string> data)
        {
            ViewModel.ReplaceSignsFromFieldData(data);
            SelectAllSigns(true);
        }

        private void MappingView_MappedDataChanged(IReadOnlyList<Dictionary<string, string>> data)
        {
            ViewModel.ReplaceSignsFromMappedData(data);
            SelectAllSigns(true);           

        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }

        private void Clipboard_ContentChanged(object? sender, object e)
        {
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }


        public async Task PrepareWindowAsync()
        {
            await ViewModel.InitializeAsync();
            TemplatesCVS.Source = ViewModel.GroupedTemplates;

            // called from app once, so while we are here
            RegisterForPrinting();
        }

        public bool HasSelectedSigns
        {
            get
            {
                return SignGridView.SelectedItems.Count > 0;
            }
        }



        private void SignTemplatesListView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (SignTemplatesListView.SelectedItem != null)
            {
                if (SignTemplatesListView.SelectedItem is SignTemplate signTemplate)
                {
                    ViewModel.SelectedTemplate = signTemplate;
                    RemoveTemplateButton.IsEnabled = signTemplate.IsCustom && IsDesigner.IsChecked == true;
                }
            }
            UpdatePrintPdfEnabled();
           
        }

        private void IsDesigner_Checked(object sender, RoutedEventArgs e)
        {
            DesignerStackPanel.Visibility = Visibility.Visible;
            AddTemplateButton.IsEnabled = true;
            RemoveTemplateButton.IsEnabled = ViewModel.IsCustomSelected;
            ExportTemplateButton.IsEnabled = true;
        }

        private void IsDesigner_Unchecked(object sender, RoutedEventArgs e)
        {
            DesignerStackPanel.Visibility = Visibility.Collapsed;
            AddTemplateButton.IsEnabled = false;
            RemoveTemplateButton.IsEnabled = false;
            ExportTemplateButton.IsEnabled = false;
        }

        private async void DesignerInfoButton_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri("https://wpkramer.github.io/church-signs/TemplateDesigner.html");
            bool success = await Sys.Launcher.LaunchUriAsync(uri);
            if (!success)
            {
                await ShowMessageAsync("Unable to open Template Designer information at this time.");
            }
        }



        private async void MainGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Check if Ctrl is pressed and the key is 'V'
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                Windows.System.VirtualKey.Control);
            bool isCtrlDown = ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (isCtrlDown && e.Key == Windows.System.VirtualKey.V)
            {
                try
                {
                    await ViewModel.PasteAsync();
                    AllSignsCheckBox.IsChecked = true;
                }
                catch (Exception ex) { await ShowMessageAsync(ex.Message); }
                e.Handled = true;
            }

        }



        public bool HasPasteData { get; private set; } = false;

        private async Task ShowMessageAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Church Signs Message",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }



        private async void TemplateGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFile file)
                    {
                        if (file.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = await ImportTemplateFile(file);
                        }
                        else if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = await ImportTemplateFile(file);
                        }
                        else
                        {
                            await ShowMessageAsync($"Rejected: {file.Name} (either a svg file or a zip file containing svg and json pairs)");
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

        private async void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (HasPasteData)
            {
                try
                {
                    await ViewModel.PasteAsync();

                    AllSignsCheckBox.IsChecked = true;

                }
                catch (Exception ex)
                {
                    await ShowMessageAsync($"{ex.GetType().Name}: {ex.Message}");
                }

            }
        }

        private async void ExportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (SignTemplatesListView.SelectedItem is SignTemplate signTemplate)
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = Path.ChangeExtension(signTemplate.Filename, "zip")
                };
                picker.FileTypeChoices.Add("Zip", [".zip"]);
                InitializeWithWindow(picker);


                StorageFile? file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await TemplateStorageService.Instance.ExportSignTemplate(signTemplate.ToStorageItem(), file);
                }
            }
        }



        private async void RemoveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedTemplate != null)
            {
                if (!ViewModel.SelectedTemplate.IsProvided)
                {
                    bool removeConfirmed = await ConfirmActionAsync($"Please confirm you want to remove {ViewModel.SelectedTemplate.Title}?");
                    if (removeConfirmed)
                    {
                        await TemplateStorageService.Instance.DeleteLocalAsync(ViewModel.SelectedTemplate.Category, ViewModel.SelectedTemplate.Filename);
                        // update so the template shows in the list
                        ViewModel.Templates.Remove(ViewModel.SelectedTemplate);
                        ViewModel.RebuildGroupedTemplates();
                        TemplatesCVS.Source = ViewModel.GroupedTemplates;
                    }
                }
            }
        }


        private async void AddTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            await ImportSignTemplateAsync();

        }

        private async Task<bool> ConfirmActionAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Content = message,
                Title = "Confirm",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.Content.XamlRoot
            };
            var dialogResult = await dialog.ShowAsync();
            return dialogResult == ContentDialogResult.Primary;
        }

        private async Task<string> ShowSignTemplateOptionsAsync(string filename)
        {
            var dialog = new ContentDialog
            {
                Title = "Template Category",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            ComboBox comboBox = new ComboBox();


            foreach (SignCategory category in Enum.GetValues<SignCategory>())
            {
                comboBox.Items.Add(category.ToString());
            }
            comboBox.SelectedIndex = 0;
            dialog.Content = comboBox;

            var dialogResult = await dialog.ShowAsync();

            if (dialogResult == ContentDialogResult.Primary)
            {
                return comboBox.SelectedValue.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private async Task ImportSignTemplateAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".svg");
            picker.FileTypeFilter.Add(".zip");

            InitializeWithWindow(picker);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                Trace.WriteLine("Manual load canceled by user.");
                return;
            }

            _ = await ImportTemplateFile(file);

        }

        private async Task<bool> ImportTemplateFile(StorageFile file)
        {
            string category = await ShowSignTemplateOptionsAsync(System.IO.Path.GetFileName(file.Path));
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }
            SignCategory signCategory;

            if (SignCategory.TryParse(category, out signCategory))
            {
                var templateStorageItems = await TemplateStorageService.Instance.ImportSignTemplatesAsync(file);
                var addedTemplates = new List<TemplateStorageItem>();
                foreach (var storageItem in templateStorageItems)
                {
                    storageItem.SignCategory = signCategory;
                    await TemplateStorageService.Instance.SaveLocalAsync(storageItem, overwrite: true);
                    addedTemplates.Add(storageItem);
                }
                if (addedTemplates.Count > 0)
                {
                    var lastTemplateAdded = ViewModel.AddTemplates(addedTemplates);
                    SignTemplatesListView.SelectedItem = lastTemplateAdded;
                    TemplatesCVS.Source = ViewModel.GroupedTemplates;
                }


            }

            return true;
        }

        private void InitializeWithWindow(object picker)
        {
            var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
            Trace.WriteLine($"Initializing picker with HWND: {hwnd}");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        private void AllSignsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectAllSigns(true);
        }

        private void SelectAllSigns(bool isSelected)
        {
            IList<object> list = SignGridView.Items;
            for (int i = 0; i < list.Count; i++)
            {
                object? item = list[i];
                SignGridView.ScrollIntoView(item);
                if (SignGridView.ContainerFromItem(item) is GridViewItem gridViewItem)
                {
                    gridViewItem.IsSelected = isSelected;
                }
            }
        }

        private void AllSignsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectAllSigns(false);
        }

        private void SignGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePrintPdfEnabled();
        }

        private void UpdatePrintPdfEnabled()
        {
            AllSignsCheckBox.Checked -= AllSignsCheckBox_Checked;
            AllSignsCheckBox.Unchecked -= AllSignsCheckBox_Unchecked;

            if (SignGridView.SelectedItems.Count == SignGridView.Items.Count)
            {
                AllSignsCheckBox.IsChecked = true;
            }
            else if (SignGridView.SelectedItems.Count == 0)
            {
                AllSignsCheckBox.IsChecked = false;
            }
            else
            {
                AllSignsCheckBox.IsChecked = null;
            }
            AllSignsCheckBox.Checked += AllSignsCheckBox_Checked;
            AllSignsCheckBox.Unchecked += AllSignsCheckBox_Unchecked;
            if (SignGridView.SelectedItems.Count > 0)
            {
                PdfExportButton.IsEnabled = true;
                PrintButton.IsEnabled = true;
            }
            else
            {
                PdfExportButton.IsEnabled = false;
                PrintButton.IsEnabled = false;
            }
        }

        private async void PdfExportButton_Click(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<ChurchSign> signs = SignGridView.SelectedItems.OfType<ChurchSign>().ToList();

            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "ChurchSigns"
            };
            picker.FileTypeChoices.Add("PDF", [".pdf"]);
            InitializeWithWindow(picker);


            StorageFile file = await picker.PickSaveFileAsync();
            if (file is null)
                return;

            await SignPdfService.ExportSelectedSignsAsync(signs, file);
        }





        #region printing

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {

            if (PrintManager.IsSupported())
            {
                try
                {
                    PrintButton.IsEnabled = false;

                    _printPreviewPages.Clear();
                    PrintCanvas.Children.Clear();

                    if (SignTemplatesListView.SelectedItem is SignTemplate signTemplate)
                    {
                        _printDefaultOrientation = signTemplate.PrintDefaultOrientation;
                        _printDefaultMediaSize = signTemplate.PrintDefaultMediaSize;
                    }


                    int totalImages = SignGridView.SelectedItems.Count;
                    bool cancelled = await ProgressDialogHelper.ShowProgressDialogAsync(
                        this.Content.XamlRoot
                        , "Generating Images For Print"
                        , totalImages,
                        async (step, token) =>
                        {
                            await GenerateImageForPrintingAsync(step, token);
                        });


                    Debug.WriteLine($"added {_printPreviewPages.Count} image pages");

                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);


                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
                    PrintButton.IsEnabled = true;
                }

                return;

            }
            else
            {
                Trace.WriteLine("Printing is not supported on this device.");
            }

        }

        private async Task GenerateImageForPrintingAsync(int step, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
            int index = step - 1;
            if (SignGridView.SelectedItems[index] is ChurchSign churchSign)
            {

                using var skBitmap = churchSign.RenderPrintSizeBitmap();
                if (skBitmap == null)
                    return;


                BitmapImage? bitmapImage = await skBitmap.ToBitmapImageAsync();
                if (bitmapImage == null)
                    return;

                var page = new Border
                {
                    Width = churchSign.PrintSize.PageWidthDips,
                    Height = churchSign.PrintSize.PageHeightDips,
                    Background = new SolidColorBrush(Colors.White),
                    Child = new Image
                    {
                        Source = bitmapImage,
                        Width = churchSign.PrintSize.PageWidthDips,
                        Height = churchSign.PrintSize.PageHeightDips,
                        Stretch = Stretch.Uniform
                    }
                };


                PrintCanvas.Children.Add(page);
                page.InvalidateMeasure();
                page.UpdateLayout();

                _printPreviewPages.Add(page);
            }

        }

        private PrintMediaSize _printDefaultMediaSize = PrintMediaSize.NorthAmericaLetter;
        private PrintOrientation _printDefaultOrientation = PrintOrientation.Portrait;
        private PrintDocument? _printDocument = null;
        private IPrintDocumentSource? _printDocumentSource = null;
        private readonly List<UIElement> _printPreviewPages = [];

        // here in case I need to register for each print job
        //private void UnRegisterForPrinting()
        //{
        //    string message = "UnRegisterForPrinting";
        //    if (_printDocument == null)
        //    {
        //        message = "UnRegisterForPrinting _printDocument is null";
        //    }
        //    else
        //    {
        //        try
        //        {
        //            if (_printDocument.DocumentSource == null)
        //            {
        //                message = "UnRegisterForPrinting _printDocument.DocumentSource is null";
        //                return;
        //            }
        //            message = "UnRegisterForPrinting Paginate";
        //            _printDocument.Paginate -= PrintDocument_Paginate;
        //            message = "UnRegisterForPrinting GetPreviewPage";
        //            _printDocument.GetPreviewPage -= PrintDocument_GetPreviewPage;
        //            message = "UnRegisterForPrinting AddPages";
        //            _printDocument.AddPages -= PrintDocument_AddPages;
        //            message = "UnRegisterForPrinting GetWindowHandle";
        //            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        //            message = "UnRegisterForPrinting PrintManagerInterop.GetForWindow";
        //            PrintManager printManager = PrintManagerInterop.GetForWindow(hWnd);
        //            message = "UnRegisterForPrinting PrintTaskRequested";
        //            printManager.PrintTaskRequested -= PrintTask_Requested;
        //            message = "UnRegisterForPrinting PrintTask_Completed";
        //        }
        //        catch (Exception ex)
        //        {
        //            message = ex.GetType().Name + " " + ex.Message;
        //        }
        //        finally
        //        {
        //            _printDocument = null;
        //            _printDocumentSource = null;
        //            _printPreviewPages.Clear();

        //        }

        //    }
        //    Trace.WriteLine(message);
        //}

        /// <summary>
        /// Run once after window is constructed
        /// </summary>
        private void RegisterForPrinting()
        {

            try
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                PrintManager printManager = PrintManagerInterop.GetForWindow(hWnd);
                printManager.PrintTaskRequested -= PrintTask_Requested;
                printManager.PrintTaskRequested += PrintTask_Requested;


                _printDocument = new PrintDocument();
                _printDocumentSource = _printDocument.DocumentSource;
                _printDocument.Paginate += PrintDocument_Paginate;
                _printDocument.GetPreviewPage += PrintDocument_GetPreviewPage;
                _printDocument.AddPages += PrintDocument_AddPages;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
            }

        }

        private void PrintTask_Requested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {

            try
            {
                Debug.WriteLine("PrintTask Requested");
                PrintTask printTask = args.Request.CreatePrintTask("Application Print", PrintTaskSourceRequested);

                // Handle PrintTask.Completed to catch failed print jobs.

                printTask.Completed += PrintTask_Completed;

                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    PrintButton.IsEnabled = false;
                });

                // Customize options displayed in print preview UI.

                IList<string> displayedOptions = printTask.Options.DisplayedOptions;


                displayedOptions.Clear();
                displayedOptions.Add(StandardPrintTaskOptions.Copies);
                displayedOptions.Add(StandardPrintTaskOptions.Orientation);
                displayedOptions.Add(StandardPrintTaskOptions.ColorMode);

                printTask.Options.Orientation = _printDefaultOrientation;
                printTask.Options.MediaSize = _printDefaultMediaSize;
            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
            }

        }

        private void PrintTaskSourceRequested(PrintTaskSourceRequestedArgs args)
        {
            // Set the document source.
            try
            {
                Debug.WriteLine("PrintTaskSourceRequested");
                args.SetSource(_printDocumentSource);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);

            }
        }

        private void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            string statusBlockText = string.Empty;

            try
            {

                if (args.Completion == PrintTaskCompletion.Failed)
                {
                    statusBlockText = "Failed to print.";
                }
                else if (args.Completion == PrintTaskCompletion.Canceled)
                {
                    statusBlockText = "Printing canceled.";
                }
                else if (args.Completion == PrintTaskCompletion.Abandoned)
                {
                    statusBlockText = "Printing abandoned.";
                }
                else
                {
                    statusBlockText = "Printing completed.";
                }
                Trace.WriteLine(statusBlockText);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
                statusBlockText = "Failed to print.";
            }

            try
            {
                if (DispatcherQueue == null)
                {

                    PrintButton.IsEnabled = true;
                    return;
                }
                bool queued = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    PrintButton.IsEnabled = true;
                });

                if (!queued)
                {

                    PrintButton.IsEnabled = true;
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);

                PrintButton.IsEnabled = true;
            }

        }

        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            try
            {
                Debug.Print("Adding all print previewpages to document");
                PrintDocument printDocument = (PrintDocument)sender;

                // Loop over all of the preview pages and add each one to be printed.
                for (int i = 0; i < _printPreviewPages.Count; i++)
                {
                    printDocument.AddPage(_printPreviewPages[i]);
                }


                // Indicate that all of the print pages have been provided.
                printDocument.AddPagesComplete();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
            }
        }

        private void PrintDocument_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            try
            {
                Debug.WriteLine("GetPreviewPage " + e.PageNumber);
                // Get the preview page for the requested page number.
                if (e.PageNumber > 0 && e.PageNumber <= _printPreviewPages.Count)
                {
                    // Set the preview page.
                    PrintDocument printDocument = (PrintDocument)sender;
                    printDocument.SetPreviewPage(e.PageNumber, _printPreviewPages[e.PageNumber - 1]);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);

            }
        }

        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            try
            {

                PrintDocument printDocument = (PrintDocument)sender;
                printDocument.SetPreviewPageCount(_printPreviewPages.Count, PreviewPageCountType.Final);
                Debug.WriteLine($"Print Preview Count set to {_printPreviewPages.Count}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
            }
        }





        #endregion


    }
}