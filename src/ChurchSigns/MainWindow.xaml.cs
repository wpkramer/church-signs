using ChurchSigns.Dialogs;
using ChurchSigns.UI.Controls;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using ChurchSigns.UI.Util;
using ChurchSigns.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using ShimSkiaSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

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
            ViewModel.MappingUpdated += (_, _) => RebuildMappingGrid();
            ViewModel.MappingReset += (_, _) => RebuildMappingGrid();
            Clipboard.ContentChanged += Clipboard_ContentChanged;
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
            this.Activated += MainWindow_Activated;
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





        private void SignTemplatesListView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (SignTemplatesListView.SelectedItem != null)
            {
                if (SignTemplatesListView.SelectedItem is SignTemplate signTemplate)
                {
                    ViewModel.SelectedTemplate = signTemplate;
                }
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
                try { await ViewModel.PasteAsync(); }
                catch (Exception ex) { await ShowMessageAsync(ex.Message); }
                e.Handled = true;
            }

        }



        private void MainGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var menuFlyout = new MenuFlyout();

            var addSignTemplateItem = new MenuFlyoutItem { Text = "Add New Sign" };
            addSignTemplateItem.Click += async (s, args) =>
            {
                await ShowMessageAsync("add selected");
            };

            var pasteItem = new MenuFlyoutItem { Text = "Paste Sign Data" };
            pasteItem.Click += async (s, args) =>
            {
                try
                {
                    await ViewModel.PasteAsync();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            };

            var separator = new MenuFlyoutSeparator();

            var item3 = new MenuFlyoutItem { Text = "Print Signs" };
            item3.Click += async (s, args) => await ShowMessageAsync("print selected");

            // Enable/disable Paste based on clipboard content
            var dataView = Clipboard.GetContent();
            pasteItem.IsEnabled = dataView.Contains(StandardDataFormats.Text);

            menuFlyout.Items.Add(addSignTemplateItem);
            menuFlyout.Items.Add(pasteItem);
            menuFlyout.Items.Add(separator);
            menuFlyout.Items.Add(item3);

            menuFlyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as FrameworkElement));
        }

        public bool HasPasteData { get; private set; } = false;

        private async Task ShowMessageAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Menu Selection",
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
                            await ShowMessageAsync( $"Accepted: {file.Name}");
                        }
                        else
                        {
                            await ShowMessageAsync($"Rejected: {file.Name} (not SVG)");
                        }
                    }
                }
            }
        }

        private void TemplateGrid_DragOver(object sender, DragEventArgs e)
        {
            if(e.DataView.Contains(StandardDataFormats.StorageItems))
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
                }
                catch(Exception ex)
                {
                    await ShowMessageAsync($"{ex.GetType().Name}: {ex.Message}");
                }

            }
        }

        #region Dynamic MappingGrid

        // The following functions and data members support the display
        // of a grid of SignTemplate Fields, Pasted Sign Columns

        /// <summary>
        /// Collection of combo boxes that allows the selection of a
        /// FieldName for the header of data pasted into the app
        /// </summary>
        private readonly List<ComboBox> _fieldSelectionBoxes = [];

        /// <summary>
        /// Map of the template to the pasted data
        /// </summary>
        private SignTemplateDataMap? _signTemplateDataMap = null;

        /// <summary>
        /// Assigns the Grid column and row for a XAML element
        /// </summary>
        private void AddControl(int col, int row, FrameworkElement ctrlToAdd)
        {
            if (col < 0 || col >= 3)
                throw new ArgumentOutOfRangeException(nameof(col));
            if (row < 0 || row >= 33)
                throw new ArgumentOutOfRangeException(nameof(row));
            Grid.SetColumn(ctrlToAdd, col);
            Grid.SetRow(ctrlToAdd, row);
            TemplateMappingGrid.Children.Add(ctrlToAdd);
        }
        private void RebuildMappingGrid()
        {
            TemplateMappingGrid.Children.Clear();
            TemplateMappingGrid.RowDefinitions.Clear();
            TemplateMappingGrid.ColumnDefinitions.Clear();
            // mapping rows
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            // data rows
            if (ViewModel.LastPaste != null)
            {
                for (int i = 0; i < ViewModel.LastPaste.Records.Count; i++)
                {
                    TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                }
            }

            int columnCount = ViewModel.SelectedTemplate.FieldNames.Count;
            columnCount = Math.Max(1, columnCount);

            if (ViewModel.LastPaste != null)
            {
                columnCount = Math.Max(1, ViewModel.LastPaste.ColumnHeaderNames.Count);
            }


            for (int i = 0; i < columnCount; i++)
            {
                TemplateMappingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }



            Thickness columnSeperation = new Thickness(2d);
            object objColHdrStyle = Application.Current.Resources["BodyStrongTextBlockStyle"];

            // if no pasted data we are just goint to show
            // our field names
            if (ViewModel.LastPaste == null)
            {
                for (int i = 0; i < columnCount; ++i)
                {
                    if (i == ViewModel.SelectedTemplate.FieldNames.Count)
                        break;

                    TextBlock textBlock = new TextBlock
                    {
                        Text = ViewModel.SelectedTemplate.FieldNames[i],
                        Margin = columnSeperation
                    };
                    if (objColHdrStyle is Style style)
                    {
                        textBlock.Style = style;
                    }
                    // add control the column head row
                    AddControl(i, 0, textBlock);
                }
                return; // we're done
            }


            _signTemplateDataMap = new SignTemplateDataMap(ViewModel.SelectedTemplate, ViewModel.LastPaste);
            columnCount = ViewModel.LastPaste.ColumnHeaderNames.Count;

            for (int i = 0; i < columnCount; ++i)
            {

                TextBlock textBlock = new TextBlock
                {
                    Text = ViewModel.LastPaste.ColumnHeaderNames[i],
                    Margin = columnSeperation
                };
                if (objColHdrStyle is Style style)
                {
                    textBlock.Style = style;
                }
                // add control the column head row
                AddControl(i, 0, textBlock);
            }
            _fieldSelectionBoxes.Clear();

            for (int pastedColIndex = 0; pastedColIndex < columnCount; ++pastedColIndex)
            {
                // ComboBox of Field Names for each pasted column
                ComboBox combo = new ComboBox
                {
                    Margin = columnSeperation,
                };

                _fieldSelectionBoxes.Add(combo);



                foreach (string fieldName in _signTemplateDataMap.DropdownFieldNames)
                {
                    combo.Items.Add(fieldName);
                }
                combo.SelectedIndex = _signTemplateDataMap.GetDropdownIndexForColumn(pastedColIndex);
                combo.Tag = pastedColIndex;
                // watch for any changes to the default;
                combo.SelectionChanged += (s, e) =>
                {

                    if (s is ComboBox comboBox)
                    {
                        if (comboBox.Tag is int columnIndex)
                        {
                            int affectedComboIndex = _signTemplateDataMap.SetDropdownIndexForColumn(columnIndex, comboBox.SelectedIndex);
                            bool showSigns = true;
                            if (affectedComboIndex >= 0 && affectedComboIndex < _fieldSelectionBoxes.Count)
                            {
                                _fieldSelectionBoxes[affectedComboIndex].SelectedIndex = 0;
                                showSigns = false; // we'll show them on the next event handler
                            }
                            if (showSigns)
                            {
                                ViewModel.Signs.Clear();
                                foreach (var fields in _signTemplateDataMap.CreateMappedRecords())
                                {
                                    ChurchSign data = new ChurchSign(_signTemplateDataMap.Template);
                                    data.Fields = fields;
                                    ViewModel.Signs.Add(data);
                                }

                            }
                        }

                    }
                };

                AddControl(pastedColIndex, 1, combo);
            }

            int rowNumber = 1;
            // Paste in the data rows
            foreach (var rowData in ViewModel.LastPaste.Records)
            {
                rowNumber += 1;
                int colNumber = 0;
                foreach (string columnData in rowData)
                {

                    TextBlock textBlock = new TextBlock
                    {
                        Text = columnData,
                        Margin = columnSeperation
                    };
                    if (objColHdrStyle is Style style)
                    {
                        textBlock.Style = style;
                    }
                    // add control the column head row
                    AddControl(colNumber++, rowNumber, textBlock);
                }
            }


            ViewModel.Signs.Clear();
            foreach (var fields in _signTemplateDataMap.CreateMappedRecords())
            {


                ChurchSign data = new ChurchSign(_signTemplateDataMap.Template);
                data.Fields = fields;
                ViewModel.Signs.Add(data);
            }

        }



        #endregion



        private async void RemoveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
           if(  ViewModel.SelectedTemplate != null )
            {
                if(!ViewModel.SelectedTemplate.IsProvided)
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
            var item = await ImportSignTemplateAsync();
            if(item is null)
            {
                return;
            }



            var template = ViewModel.AddLocalTemplate(item);
            if (template is null)
            {
                await ShowMessageAsync("Could not use that SVG as a template.");
                return;
            }

          

            // update so the template shows in the list
            TemplatesCVS.Source = ViewModel.GroupedTemplates;
            // select template so it shows in the sign list and the field mapping
            SignTemplatesListView.SelectedItem = template;
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

            if(dialogResult == ContentDialogResult.Primary)
            {
                return comboBox.SelectedValue.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private async Task<TemplateStorageItem?> ImportSignTemplateAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".svg");

            InitializeWithWindow(picker);

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                Trace.WriteLine("Manual load canceled by user.");
                return null;
            }

            string category = await ShowSignTemplateOptionsAsync(System.IO.Path.GetFileName(file.Path));
            if (string.IsNullOrEmpty(category))
            {
                return null;
            }
            SignCategory signCategory;
            if (SignCategory.TryParse(category, out signCategory))
            {
                TemplateStorageItem storageItem = new TemplateStorageItem
                {
                    Content = await FileIO.ReadTextAsync(file),
                    IsProvided = false,
                    SignCategory = signCategory,
                    Filename = file.Name,
                };

                await TemplateStorageService.Instance.SaveLocalAsync(storageItem, true);

                return storageItem;
            }


            return null;

        }

        private void InitializeWithWindow(object picker)
        {
            var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
            Trace.WriteLine($"Initializing picker with HWND: {hwnd}");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        private async void EditTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SignTemplate template)
            {
                var dialog = new SignTemplateDialog(template);
                dialog.XamlRoot = this.Content.XamlRoot;
                var dialogResult = await dialog.ShowAsync();
                if(dialogResult == ContentDialogResult.Primary)
                {
                    dialog.ViewModel.UpdateTemplate();
                    // Save the changes to the template
                    await TemplateStorageService.Instance.SaveLocalAsync(template.ToStorageItem(), true);
                    // Update the ViewModel to reflect the changes
                    ViewModel.RebuildGroupedTemplates();
                    TemplatesCVS.Source = ViewModel.GroupedTemplates;
                }

            }
        }



        private void AllSignsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in SignGridView.Items)
            {
                if (SignGridView.ContainerFromItem(item) is GridViewItem gridViewItem)
                {
                    gridViewItem.IsSelected = true;
                }
            }
        }

        private void AllSignsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in SignGridView.Items)
            {
                if (SignGridView.ContainerFromItem(item) is GridViewItem gridViewItem)
                {
                    gridViewItem.IsSelected = false;
                }
            }
        }

        private void SignGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllSignsCheckBox.Checked -= AllSignsCheckBox_Checked;
            AllSignsCheckBox.Unchecked -= AllSignsCheckBox_Unchecked;

            if ( SignGridView.SelectedItems.Count == SignGridView.Items.Count)
            {
                AllSignsCheckBox.IsChecked = true;
            }
            else if(SignGridView.SelectedItems.Count == 0)
            {
                AllSignsCheckBox.IsChecked = false;
            }
            else
            {
                AllSignsCheckBox.IsChecked = null;
            }
            AllSignsCheckBox.Checked += AllSignsCheckBox_Checked;
            AllSignsCheckBox.Unchecked += AllSignsCheckBox_Unchecked;
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
                        _printDefaultOrientation = signTemplate.SignOrientation;
                        _printDefaultMediaSize = signTemplate.MediaSize;
                    }
                    

                    foreach (ChurchSign churchSign in SignGridView.SelectedItems.OfType<ChurchSign>())
                    {

                        using var skBitmap = churchSign.RenderPrintSizeBitmap();
                        if (skBitmap == null)
                            continue;

                        // XAML uses DIPs: 96 per inch — NOT PDF points (72 per inch)

                        // TODO: Create serveral classes based on Size or SizeF
                        // 
                        // SizeInches, SizePDFPrint, SizeXAMLThumbnail, SizeXAMLDisplay, SizeXAMLPrint 

                        double widthDips = churchSign.PrintSize.PageWidthInches * 96.0;
                        double heightDips = churchSign.PrintSize.PageHeightInches * 96.0;
                        BitmapImage? bitmapImage = await skBitmap.ToBitmapImageAsync();
                        if (bitmapImage == null)
                            continue;

                        var page = new Border
                        {
                            Width = widthDips,
                            Height = heightDips,
                            Background = new SolidColorBrush(Colors.White),
                            Child = new Image
                            {
                                Source = bitmapImage,
                                Width = widthDips,
                                Height = heightDips,
                                Stretch = Stretch.Uniform
                            }
                        };


                        PrintCanvas.Children.Add(page);
                        page.InvalidateMeasure();
                        page.UpdateLayout();

                        _printPreviewPages.Add(page);


                    }

                    
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


        private PrintMediaSize _printDefaultMediaSize = PrintMediaSize.NorthAmericaLetter;
        private PrintOrientation _printDefaultOrientation = PrintOrientation.Portrait;
        private PrintDocument? _printDocument = null;
        private IPrintDocumentSource? _printDocumentSource = null;
        private readonly List<UIElement> _printPreviewPages = [];


        private void UnRegisterForPrinting()
        {
            string message = "UnRegisterForPrinting";
            if (_printDocument == null)
            {
                message = "UnRegisterForPrinting _printDocument is null";
            }
            else
            {
                try
                {
                    if (_printDocument.DocumentSource == null)
                    {
                        message = "UnRegisterForPrinting _printDocument.DocumentSource is null";
                        return;
                    }
                    message = "UnRegisterForPrinting Paginate";
                    _printDocument.Paginate -= PrintDocument_Paginate;
                    message = "UnRegisterForPrinting GetPreviewPage";
                    _printDocument.GetPreviewPage -= PrintDocument_GetPreviewPage;
                    message = "UnRegisterForPrinting AddPages";
                    _printDocument.AddPages -= PrintDocument_AddPages;
                    message = "UnRegisterForPrinting GetWindowHandle";
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    message = "UnRegisterForPrinting PrintManagerInterop.GetForWindow";
                    PrintManager printManager = PrintManagerInterop.GetForWindow(hWnd);
                    message = "UnRegisterForPrinting PrintTaskRequested";
                    printManager.PrintTaskRequested -= PrintTask_Requested;
                    message = "UnRegisterForPrinting PrintTask_Completed";
                }
                catch (Exception ex)
                {
                    message = ex.GetType().Name + " " + ex.Message;
                }
                finally
                {
                    _printDocument = null;
                    _printDocumentSource = null;
                    _printPreviewPages.Clear();

                }

            }
            Trace.WriteLine(message);
        }

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
            // Create the PrintTask.
            // Defines the title and delegate for PrintTaskSourceRequested.
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
                // Get the list of displayed options.
                IList<string> displayedOptions = printTask.Options.DisplayedOptions;

                // Choose the printer options to be shown.
                // The order in which the options are appended determines
                // the order in which they appear in the UI.
                displayedOptions.Clear();
                displayedOptions.Add(StandardPrintTaskOptions.Copies);
                displayedOptions.Add(StandardPrintTaskOptions.Orientation);
                displayedOptions.Add(StandardPrintTaskOptions.ColorMode);
                //displayedOptions.Add(StandardPrintTaskOptions.Collation);
                //displayedOptions.Add(StandardPrintTaskOptions.Duplex);

                // Preset the default value of the print media size option.
                //  Maybe future adjust generated image to fit media size
                //  printTask.Options.MediaSize = _printDefaultMediaSize;
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
                // Notify the user if the print operation fails.
                // StatusBlock.Text = "Failed to print.";
                // TODO: on the page show failed print status
            }
        }

        private void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            string statusBlockText = string.Empty;
            // TODO: on the page show print status
            try
            {
                // Notify the user if the print operation fails.
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
                    // If the DispatcherQueue is not available, update the UI directly.
                    // StatusBlock.Text = statusBlockText;
                    PrintButton.IsEnabled = true;
                    return;
                }
                bool queued = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    //  StatusBlock.Text = statusBlockText;
                    PrintButton.IsEnabled = true;
                });

                if (!queued)
                {
                    // If the DispatcherQueue is not available, update the UI directly.
                    // StatusBlock.Text = statusBlockText;
                    PrintButton.IsEnabled = true;
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);

                // If the DispatcherQueue is not available, update the UI directly.
                // StatusBlock.Text = statusBlockText;
                PrintButton.IsEnabled = true;
            }
            finally
            {
                // we only register once, and keep it registered
        //        UnRegisterForPrinting();
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
                // TODO: Notify the user if the print operation fails.
                // StatusBlock.Text = "Failed to print.";
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
                // TODO: Notify the user if the print operation fails.
                // StatusBlock.Text = "Failed to print.";
            }
        }

        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            try
            {

                //// Get the PrintTaskOptions.
                //PrintTaskOptions printingOptions = ((PrintTaskOptions)e.PrintTaskOptions);

                //// Get the page description to determine the size of the print page.
                //// might need to add this to our future object construction
                //PrintPageDescription pageDescription = printingOptions.GetPageDescription(0);
                //double pageWidthDips = pageDescription.PageSize.Width;
                //double pageHeightDips = pageDescription.PageSize.Height;


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