using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Printing;


namespace ChurchSigns.UI.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {

        private readonly IClipboardService _clipboard;
        private SignTemplate _selectedTemplate = CreateBlankTemplate();
        private PastedRecordData _lastTablePaste = new PastedRecordData();
        private PastedFieldPairs _lastFieldPairs = new PastedFieldPairs();

        public ObservableCollection<SignTemplate> Templates { get; } = [];
        public ObservableCollection<ChurchSign> Signs { get; } = [];
        public ObservableCollection<GroupInfoList>? GroupedTemplates { get; private set; }

        public XamlRoot? XamlRoot { get; set; }
        public WindowId WindowId { get; set; }
        public Window? MainWindow { get; set; }
        private nint _hwnd = new();

        public MainViewModel()
        {
            _clipboard = new WindowsClipboardService();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PastedRecordData LastTablePaste
        {
            get => _lastTablePaste;
            set
            {
                if (value != _lastTablePaste)
                {
                    _lastTablePaste = value;
                    OnPropertyChanged(nameof(LastTablePaste));
                }
            }
        }

        public PastedFieldPairs LastFieldPairs
        {
            get => _lastFieldPairs;
            set
            {
                if (value != _lastFieldPairs)
                {
                    _lastFieldPairs = value;
                    OnPropertyChanged(nameof(LastFieldPairs));
                }
            }
        }

        private bool _isDesignerMode;
        public bool IsDesignerMode
        {
            get => _isDesignerMode;
            set
            {
                _isDesignerMode = value;

                OnPropertyChanged(nameof(IsDesignerMode));
            }
        }



        // Remove enabled when designer + custom template
        public bool CanRemoveSelectedTemplate =>
            IsDesignerMode && SelectedTemplate is { IsProvided: false };

        public SignTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (ReferenceEquals(_selectedTemplate, value) || value is null)
                    return;

                _selectedTemplate = value;
                // clear out the pasted data for a different template
                LastTablePaste = new PastedRecordData();
                LastFieldPairs = new PastedFieldPairs();
                OnPropertyChanged(nameof(IsCustomSelected));
                OnPropertyChanged(nameof(SelectedTemplate));
                OnPropertyChanged(nameof(MultiSignVisibility));
                OnPropertyChanged(nameof(SingleSignVisibility));
            }
        }


        public async Task<bool> ImportTemplateFileAsync(string filepath)
        {
            string category = await ShowSignTemplateOptionsAsync(System.IO.Path.GetFileName(filepath));
            if (string.IsNullOrEmpty(category))
            {
                return false;
            }
            SignCategory signCategory;

            if (SignCategory.TryParse(category, out signCategory))
            {
                var templateStorageItems = TemplateFileService.Read(filepath);
                var addedTemplates = new List<TemplateStorageItem>();
                foreach (var storageItem in templateStorageItems)
                {
                    storageItem.SignCategory = signCategory;
                    TemplateFileService.Instance.Save(storageItem);
                    addedTemplates.Add(storageItem);
                }
                if (addedTemplates.Count > 0)
                {
                    var lastTemplateAdded = AddTemplates(addedTemplates);
                    if (lastTemplateAdded != null)
                    {
                        SelectedTemplate = lastTemplateAdded;
                    }
                }


            }

            return true;
        }

        private async Task<string> ShowSignTemplateOptionsAsync(string filename)
        {
            var dialog = new ContentDialog
            {
                Title = "Template Category",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
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

        public Visibility MultiSignVisibility
        {
            get
            {
                if (SelectedTemplate == null)
                    return Visibility.Collapsed;
                if (SelectedTemplate.SignMode == TemplateSignMode.SingleSign)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        public Visibility SingleSignVisibility
        {
            get
            {
                if (SelectedTemplate == null)
                    return Visibility.Collapsed;
                if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }


        public bool IsCustomSelected
        {
            get
            {
                if (_selectedTemplate != null)
                {

                    return !_selectedTemplate.IsProvided;
                }
                return false;
            }
        }


        public async Task InitializeAsync(nint hwnd)
        {
            _hwnd = hwnd;
            RegisterForPrinting();

            Templates.Clear();
            foreach (var item in TemplateFileService.Instance.GetProvidedTemplates())
                TryAddTemplate(item);
            foreach (var item in TemplateFileService.Instance.GetUserTemplates())
                TryAddTemplate(item);

            RebuildGroupedTemplates();

        }
        private bool IsPlacehoderSign(IReadOnlyList<Dictionary<string, string>> data)
        {
            return data.Count == 0 || (data.Count == 1 && data[0].Count == 0);
        }

        private SignTemplate? _lastMessagedTemplate = null;
        public async Task<bool> ReplaceSignsFromMappedDataAsync(IReadOnlyList<Dictionary<string, string>> data)
        {
            if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
            {
                Signs.Clear();
                if (IsPlacehoderSign(data))
                {
                    var placeholder = ChurchSign.CreatePlaceholderSign(SelectedTemplate);
                    Signs.Add(placeholder);
                    return false;
                }
                foreach (var row in data)
                {
                    if (row is null || row.Count == 0) continue;
                    var sign = new ChurchSign(SelectedTemplate, row);
                    Signs.Add(sign);
                }
                if (Signs.Count == 0)  // do I ever get here??
                {
                    var placeholder = ChurchSign.CreatePlaceholderSign(SelectedTemplate);
                    Signs.Add(placeholder);
                    return false;
                }
                // see if anything was mapped
                if (Signs[0].HasNoMappings)
                {
                    // try not to be annoying, message only once for the selected template
                    if (_lastMessagedTemplate == null || (object)_lastMessagedTemplate != (object)SelectedTemplate)
                    {
                        await ShowMessageAsync("Please use the drop downs to choose what pasted data column(s) are associated with a sign field.");
                        _lastMessagedTemplate = SelectedTemplate;
                    }
                    return false;
                }
                return true;
            }
            return false;
        }


        public SignTemplate? AddTemplates(IEnumerable<TemplateStorageItem> storageItems)
        {
            SignTemplate? lastAdded = null;
            List<SignTemplate> templates = [];
            foreach (var storageItem in storageItems)
            {
                var template = new SignTemplate(storageItem);
                if (!template.IsValid)
                    continue;

                templates.Add(template);
                lastAdded = template;
            }
            UpsertTemplates(templates);
            return lastAdded;
        }

        private void UpsertTemplates(IEnumerable<SignTemplate> incoming)
        {
            // TODO: don't affect provided
            foreach (var template in incoming.Where(t => t.IsValid))
            {
                var existingIndex = IndexOfTemplate(template.Category, template.Filename);
                if (existingIndex >= 0)
                    Templates[existingIndex] = template; // replace
                else
                    Templates.Add(template);
            }

            RebuildGroupedTemplates();
        }

        private int IndexOfTemplate(SignCategory category, string filename)
        {
            for (int i = 0; i < Templates.Count; i++)
            {
                var t = Templates[i];
                if (!t.IsProvided && t.Category == category
                    && string.Equals(t.Filename, filename, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public void RebuildGroupedTemplates()
        {
            GroupedTemplates = new ObservableCollection<GroupInfoList>(
                from t in Templates
                orderby t.Filename
                group t by t.Group into g
                orderby g.Key
                select new GroupInfoList(g.Key, g));

            OnPropertyChanged(nameof(GroupedTemplates));
        }

        public async Task PasteAsync()
        {
            var text = await _clipboard.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Clipboard does not contain text for signs.");

            ApplyPaste(text);
        }

        private void ApplyPaste(string clipboardText)
        {
            if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
            {
                LastTablePaste = new PastedRecordData(clipboardText);
                LastFieldPairs = new PastedFieldPairs();
            }
            else
            {
                LastTablePaste = new PastedRecordData();
                LastFieldPairs = new PastedFieldPairs(clipboardText);
            }
        }

        private void TryAddTemplate(TemplateStorageItem item)
        {
            var t = new SignTemplate(item);
            if (t.IsValid)
            {
                Templates.Add(t);

            }

        }

        private static SignTemplate CreateBlankTemplate() =>
            new(new TemplateStorageItem
            {
                Content = """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"></svg>
                    """,
                IsProvided = true,
                SignCategory = SignCategory.Miscellaneous,
                Filename = "No Template Selected.svg",
            });

        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));



        public async Task ShowMessageAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Church Signs Message",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        public async Task RemoveSelectedTemplateAsync()
        {
            if (SelectedTemplate != null)
            {
                if (!SelectedTemplate.IsProvided)
                {
                    bool removeConfirmed = await ConfirmActionAsync($"Please confirm you want to remove {SelectedTemplate.Title}?");
                    if (removeConfirmed)
                    {                        
                        TemplateFileService.Instance.Delete(SelectedTemplate.ToStorageItem());
                        // update the list
                        Templates.Remove(SelectedTemplate);
                        RebuildGroupedTemplates();
                    }
                }
            }
        }

        private async Task<bool> ConfirmActionAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Content = message,
                Title = "Confirm",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = XamlRoot
            };
            var dialogResult = await dialog.ShowAsync();
            return dialogResult == ContentDialogResult.Primary;
        }

        public async Task<bool> ImportSignTemplateAsync()
        {
            bool importedTemplate = false;
            var picker = new FileOpenPicker(WindowId)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".svg");
            picker.FileTypeFilter.Add(".zip");

            var pickFileResult = await picker.PickSingleFileAsync();
            if (pickFileResult != null)
            {

                importedTemplate = await ImportTemplateFileAsync(pickFileResult.Path);
            }
            else
            {
                Trace.WriteLine("Manual load canceled by user.");

            }
            return importedTemplate;
        }

        public async Task ExportSelectedTemplateAsync()
        {
            try
            {
                if (SelectedTemplate is null)
                    return;


                var savePicker = new FileSavePicker(this.WindowId)
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                    SuggestedFileName = System.IO.Path.ChangeExtension(SelectedTemplate.Filename, "zip"),
                    CommitButtonText = "Save Template",
                    FileTypeChoices =
                    {
                        {"Zip", new List<string> {".zip" } }
                    },
                    DefaultFileExtension = ".zip"
                };

                // Show the picker
                var pickerResult = await savePicker.PickSaveFileAsync();
                if (pickerResult is null)
                    return; // cancel

                TemplateFileService.Export(SelectedTemplate.ToStorageItem(), pickerResult.Path);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().Name + " " + ex.Message);
            }
        }


        #region Sign PDF or Print
        private PrintMediaSize _printDefaultMediaSize = PrintMediaSize.NorthAmericaLetter;
        private PrintOrientation _printDefaultOrientation = PrintOrientation.Portrait;
        private PrintDocument? _printDocument = null;
        private IPrintDocumentSource? _printDocumentSource = null;
        private readonly List<UIElement> _printPreviewPages = [];
        private IReadOnlyList<ChurchSign>? _signsForPrinting = null;
        private Canvas? _printCanvas = null;

        public async Task ExportPdfSignsAsync(IReadOnlyList<ChurchSign> signs)
        {
            var savePicker = new FileSavePicker(this.WindowId)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = "ChurchSigns"
            };
            savePicker.FileTypeChoices.Add("PDF", [".pdf"]);
            // Show the picker
            var pickerResult = await savePicker.PickSaveFileAsync();
            if (pickerResult is null)
                return; // cancel


            await SignPdfService.ExportSelectedSignsAsync(signs, pickerResult.Path);
        }


        /// <summary>
        /// Run once after window is constructed
        /// </summary>
        private void RegisterForPrinting()
        {

            try
            {
                PrintManager printManager = PrintManagerInterop.GetForWindow(_hwnd);
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


        public async Task PrintSignsAsync(IReadOnlyList<ChurchSign> signs, Canvas printCanvas)
        {
            ArgumentNullException.ThrowIfNull(signs);
            ArgumentNullException.ThrowIfNull(printCanvas);
            if (XamlRoot == null)
                throw new ApplicationException("MainViewModel has no XAML root");
            if (!PrintManager.IsSupported())
                throw new ApplicationException("Printing not supported on this device");

            if (signs.Count == 0)
                return;

            try
            {
                _signsForPrinting = signs;
                _printCanvas = printCanvas;
                _printPreviewPages.Clear();

                if (SelectedTemplate != null)
                {
                    _printDefaultOrientation = SelectedTemplate.PrintDefaultOrientation;
                    _printDefaultMediaSize = SelectedTemplate.PrintDefaultMediaSize;
                    _signsForPrinting = signs; // remove this
                }


                int totalImages = signs.Count;
                bool cancelled = await ProgressDialogHelper.ShowProgressDialogAsync(
                    this.XamlRoot
                    , "Generating Images For Print"
                    , totalImages,
                    async (step, token) =>
                    {
                        await GenerateImageForPrintingAsync(step, token);
                    });


                Debug.WriteLine($"added {_printPreviewPages.Count} image pages");
                

                await PrintManagerInterop.ShowPrintUIForWindowAsync(_hwnd);


            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType().Name + " " + ex.Message);
            }

            return;

        }

            
        



        private async Task GenerateImageForPrintingAsync(int step, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                throw new TaskCanceledException();
            int index = step - 1;
            if (_signsForPrinting?[index] is ChurchSign churchSign)
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


                _printCanvas?.Children.Add(page);
                page.InvalidateMeasure();
                page.UpdateLayout();

                _printPreviewPages.Add(page);
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
