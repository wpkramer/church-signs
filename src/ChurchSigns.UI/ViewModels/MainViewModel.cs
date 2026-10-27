using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using LegacyPickers = Windows.Storage.Pickers; // Only for PickerLocationId enum


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

        // MainViewModel
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



        // TODO: 3. Prefer VM over export / remove events

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


        public async Task<bool> ImportTemplateFileAsync(StorageFile file)
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


        public async Task InitializeAsync()
        {
            await TemplateStorageService.Instance.EnsureLocalFolderStructureAsync();

            Templates.Clear();
            foreach (var item in await TemplateStorageService.Instance.GetContentTemplatesAsync())
                TryAddTemplate(item);
            foreach (var item in await TemplateStorageService.Instance.GetLocalTemplatesAsync())
                TryAddTemplate(item);

            RebuildGroupedTemplates();

        }

        //private void SortGroupTemplates()
        //{
        //    var grouped =
        //            from item in Templates
        //            group item by item.Group into g
        //            orderby g.Key
        //            select new GroupInfoList(g.Key, g);

        //    GroupedTemplates = new ObservableCollection<GroupInfoList>(grouped);

        //    OnPropertyChanged(nameof(GroupedTemplates));
        //}

        public void ReplaceSignsFromMappedData(IReadOnlyList<Dictionary<string, string>> data)
        {
            if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
            {
                Signs.Clear();
                if (data.Count == 0 || (data.Count == 1 && data[0].Count == 0))
                {
                    var placeholder = ChurchSign.CreatePlaceholderSign(SelectedTemplate);
                    Signs.Add(placeholder);
                    return;
                }
                foreach (var row in data)
                {
                    if (row is null || row.Count == 0) continue;
                    var sign = new ChurchSign(SelectedTemplate, row);

                    Signs.Add(sign);
                }
                if (Signs.Count == 0)
                {
                    var placeholder = ChurchSign.CreatePlaceholderSign(SelectedTemplate);
                    Signs.Add(placeholder);
                }
            }
        }

        public void ReplaceSignsFromFieldData(IReadOnlyDictionary<string, string> data)
        {
            if (SelectedTemplate.SignMode == TemplateSignMode.SingleSign)
            {
                Signs.Clear();

                Signs.Add(new ChurchSign(SelectedTemplate, data));

            }
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

        //private void UpdatePrintPdfEnabled()
        //{
        //    AllSignsCheckBox.Checked -= AllSignsCheckBox_Checked;
        //    AllSignsCheckBox.Unchecked -= AllSignsCheckBox_Unchecked;

        //    if (SignGridView.SelectedItems.Count == SignGridView.Items.Count)
        //    {
        //        AllSignsCheckBox.IsChecked = true;
        //    }
        //    else if (SignGridView.SelectedItems.Count == 0)
        //    {
        //        AllSignsCheckBox.IsChecked = false;
        //    }
        //    else
        //    {
        //        AllSignsCheckBox.IsChecked = null;
        //    }
        //    AllSignsCheckBox.Checked += AllSignsCheckBox_Checked;
        //    AllSignsCheckBox.Unchecked += AllSignsCheckBox_Unchecked;
        //    if (SignGridView.SelectedItems.Count > 0)
        //    {
        //        PdfExportButton.IsEnabled = true;
        //        PrintButton.IsEnabled = true;
        //    }
        //    else
        //    {
        //        PdfExportButton.IsEnabled = false;
        //        PrintButton.IsEnabled = false;
        //    }
        //}

        public void InitializeWithWindow(object picker)
        {
            //var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
            //Trace.WriteLine($"Initializing picker with HWND: {hwnd}");

            var hwnd = Win32Interop.GetWindowFromWindowId(this.WindowId);

            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

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
                        await TemplateStorageService.Instance.DeleteLocalAsync(SelectedTemplate.Category, SelectedTemplate.Filename);
                        // update so the template shows in the list
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
                StorageFile? file = await StorageFile.GetFileFromPathAsync(pickFileResult.Path);
                if (file != null)
                {
                    _ = await ImportTemplateFileAsync(file);
                    importedTemplate = true;
                }
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

                if(!File.Exists(pickerResult.Path))
                {
                    using(var fs = File.Create(pickerResult.Path))
                    {
                        // no content needed - just touching the file
                    }
                }

                // Now safely get the StorageFile
                StorageFile pickedFile = await StorageFile.GetFileFromPathAsync(pickerResult.Path);

                await TemplateStorageService.Instance.ExportSignTemplate(SelectedTemplate.ToStorageItem(), pickedFile);


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.GetType().Name + " " + ex.Message);
            }
        }

    }
}
