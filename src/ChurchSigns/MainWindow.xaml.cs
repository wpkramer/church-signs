using ChurchSigns.UI.Models;
using ChurchSigns.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

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
            if (SignTemplatesListView.SelectedItem == null)
                return;
            if (SignTemplatesListView.SelectedItem is SignTemplate signTemplate)
            {
                ViewModel.SelectedTemplate = signTemplate;
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


            {   // add title to the first row
                TextBlock hdrTextBlock = new()
                {
                    Text = "Pasted Sign Data",
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                object objHeaderStyle = Application.Current.Resources["SubtitleTextBlockStyle"];
                if (objHeaderStyle is Style style)
                {
                    hdrTextBlock.Style = style;
                }
                // default to column 0, row 0,
                Grid.SetColumnSpan(hdrTextBlock, columnCount);
                TemplateMappingGrid.Children.Add(hdrTextBlock);
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
                    AddControl(i, 1, textBlock);
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
                AddControl(i, 1, textBlock);
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

                AddControl(pastedColIndex, 2, combo);
            }

            int rowNumber = 2;
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
    }
}