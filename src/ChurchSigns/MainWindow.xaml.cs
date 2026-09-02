using ChurchSigns.UI.Models;
using ChurchSigns.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WinRT.Interop;
using ChurchSigns.UI.Services;
using ChurchSigns.Dialogs;

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
        }

        private void Clipboard_ContentChanged(object? sender, object e)
        {
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }

        // One more CsWinRT1030 that I can't seem to code around, some help please.
        // caused by TemplatesCVS.Source = ViewModel.GroupedTemplates;
        public async Task InitializeTemplatesAsync()
        {
            await ViewModel.InitializeAsync();
            TemplatesCVS.Source = ViewModel.GroupedTemplates;

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
                                    SignData data = new SignData(_signTemplateDataMap.Template);
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


                SignData data = new SignData(_signTemplateDataMap.Template);
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
    }
}