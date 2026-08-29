using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using ChurchSigns.UI.Util;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ChurchSigns
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            Signs = [];
            Templates = [];
        }


        /// <summary>
        /// Called from the App to load our templates.
        /// </summary>
        /// <remarks>
        /// Just test data now until I add copy and paste logic
        /// </remarks>
        public async Task InitializeTemplatesAsync()
        {
            await TemplateStorageService.Instance.EnsureLocalFolderStructureAsync();
            var content = await TemplateStorageService.Instance.GetContentTemplatesAsync();
            var local = await TemplateStorageService.Instance.GetLocalTemplatesAsync();

            foreach (var templateStorageItem in content)
            {
                SignTemplate tmplt = new SignTemplate(templateStorageItem);
                if (tmplt.IsValid)
                {
                    Templates.Add(tmplt);
                }
            }

            foreach (var templateStorageItem in local)
            {
                SignTemplate tmplt = new SignTemplate(templateStorageItem);
                if (tmplt.IsValid)
                {
                    Templates.Add(tmplt);
                }
            }

            TemplatesCVS.Source = await GetSignTemplatesGroupedAsync();

        }



 

        public async Task<ObservableCollection<GroupInfoList>> GetSignTemplatesGroupedAsync()
        {
            // Grab Contact objects from pre-existing list (list is returned from function GetContactsAsync())
            var query = from item in Templates
                        group item by item.Group into g
                        orderby g.Key
                        select new GroupInfoList(g) { Key = g.Key };

            return new ObservableCollection<GroupInfoList>(query);
        }

        private const string blankSvg = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<!DOCTYPE svg PUBLIC ""-//W3C//DTD SVG 1.1//EN"" ""http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd"">
<svg width=""100%"" height=""100%"" viewBox=""0 0 1000 1000"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" xml:space=""preserve"" xmlns:serif=""http://www.serif.com/"" style=""fill-rule:evenodd;clip-rule:evenodd;stroke-linejoin:round;stroke-miterlimit:2;"">
</svg>";

        private SignTemplate _selectedTemplate = new(
            new TemplateStorageItem
            {
                Content = blankSvg,
                IsProvided = true,
                SignCategory = SignCategory.Miscellaneous,
                Filename = "No Template Selected.svg",
            }
            );


        public SignTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                ResetFieldMap();
                InitializeDataTable();
            }
        }

        private void InitializeDataTable()
        {
            //throw new NotImplementedException();
        }

        private void ResetFieldMap()
        {
            _fieldNameMap.Clear();
            foreach(string key in _selectedTemplate.FieldNames)
            {
                if (string.IsNullOrEmpty(key))
                    continue;
                _fieldNameMap.Add(key, string.Empty);
            }
        }

        public ObservableCollection<SignData> Signs { get; private set; }
        public ObservableCollection<SignTemplate> Templates { get; private set; }

        // map of fields found in sign template to column names that were pasted in
        private Dictionary<string,string> _fieldNameMap = new Dictionary<string,string>();


        // Detect Ctrl+V key combination
        private async void Window_KeyDown(object sender, KeyRoutedEventArgs e)
        {

        }

        // Reads text from clipboard and displays it
        private async Task PasteFromClipboardAsync()
        {
            try
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Text))
                {
                    string clipboardText = await dataPackageView.GetTextAsync();
                    var pastedRecord = new PastedRecordData(clipboardText);
                    LoadTemplateMappingGrid(SelectedTemplate, pastedRecord);
                }
                else
                {
                    ShowMessage("Clipboard does not contain text for signs");                   
                }
            }
            catch (Exception ex)
            {
               // OutputBox.Text = $"Error reading clipboard: {ex.Message}";
            }
        }



        private PastedRecordData? _lastRecordData = null;


        // reload template map into grid
        private void LoadTemplateMappingGrid(SignTemplate signTemplate, PastedRecordData? pastedData = null)
        {




            TemplateMappingGrid.Children.Clear();
            TemplateMappingGrid.RowDefinitions.Clear();
            TemplateMappingGrid.ColumnDefinitions.Clear();
            // mapping rows
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            // data rows
            if (pastedData != null)
            {
                for (int i = 0; i < pastedData.Records.Count; i++)
                {
                    TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                }
            }

            int columnCount = signTemplate.FieldNames.Count;
            columnCount = Math.Max(1, columnCount);

            if(pastedData != null)
            {
                columnCount = Math.Max(1, pastedData.ColumnHeaderNames.Count);
            }


            for (int i = 0; i < columnCount; i++)
            {
                TemplateMappingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength (1, GridUnitType.Star) });
            }

            
            {   // add title to the first row
                TextBlock hdrTextBlock = new TextBlock
                {
                    Text = "Pasted Sign Data",
                    HorizontalAlignment = HorizontalAlignment.Center,
                };
                object objHeaderStyle = Application.Current.Resources["SubtitleTextBlockStyle"];
                if(objHeaderStyle is Style style)
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
            if (pastedData == null)
            {
                for (int i = 0; i < columnCount; ++i)
                {
                    if (i == signTemplate.FieldNames.Count)
                        break;

                    TextBlock textBlock = new TextBlock
                    {
                        Text = signTemplate.FieldNames[i],
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


            _signTemplateDataMap = new SignTemplateDataMap(signTemplate, pastedData);
            columnCount = pastedData.ColumnHeaderNames.Count;
            
            for (int i = 0; i < columnCount; ++i)
            {

                TextBlock textBlock = new TextBlock
                {
                    Text = pastedData.ColumnHeaderNames[i],
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


                
                foreach(string fieldName in _signTemplateDataMap.DropdownFieldNames)
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
                            if(showSigns)
                            {
                                Signs.Clear();
                                foreach(var fields in _signTemplateDataMap.CreateMappedRecords())
                                {
                                    SignData data = new SignData(_signTemplateDataMap.Template);
                                    data.Fields = fields;
                                    Signs.Add(data);
                                }

                            }
                        }
                        
                    }
                };

                AddControl(pastedColIndex, 2, combo);
            }

            int rowNumber = 2;
            // Paste in the data rows
            foreach(var rowData in pastedData.Records)
            {
                rowNumber += 1;
                int colNumber = 0;
                foreach(string columnData in rowData)
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


            Signs.Clear();
            foreach (var fields in _signTemplateDataMap.CreateMappedRecords())
            {


                SignData data = new SignData(_signTemplateDataMap.Template);
                data.Fields = fields;
                Signs.Add(data);
            }

        }
        private List<ComboBox> _fieldSelectionBoxes = new List<ComboBox>();
        private SignTemplateDataMap? _signTemplateDataMap = null;

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

        private void SignTemplatesListView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (SignTemplatesListView.SelectedItem == null)
                return;
            if (SignTemplatesListView.SelectedItem is SignTemplate signTemplate)
            {
                SelectedTemplate = signTemplate;
                // temporary for testing
                //  LoadSigns(signTemplate);
                LoadTemplateMappingGrid(signTemplate);
            }
        }



        private void LoadSigns(SignTemplate signTemplate)
        {
            ArgumentNullException.ThrowIfNull(signTemplate);


            Signs.Clear();
            var sd = new SignData(signTemplate);
            sd.Fields.Add("Name", "Kramer");
            Signs.Add(sd);

            for (int i = 0; i < 100; i++)
            {
                sd = new SignData(signTemplate);
                sd.Fields.Add("Name", $"Jones #{i}");
                Signs.Add(sd);
            }


            //templateStorageItem.SvgSignTemplate = await FileIO.ReadTextAsync(svgFile);
            //SignData templateStorageItem = new()
            //{
            //    Title = "Sign SvgSignTemplate"
            //};

            //await ReadContentFileAsync(templateStorageItem);

            //Signs.Add(templateStorageItem);

            //Signs.Add(new SignData
            //{
            //    Title = "William Kramer",
            //    SvgSignTemplate = templateStorageItem.SvgSignTemplate,
            //    Fields = new Dictionary<string, string>
            //    {
            //        { "Name", "William Kramer" }
            //    }
            //});
            //Signs.Add(new SignData
            //{
            //    Title = "John Smith",
            //    SvgSignTemplate = templateStorageItem.SvgSignTemplate,
            //    Fields = new Dictionary<string, string>
            //    {
            //        { "Name", "Smith" }
            //    }
            //});
            //for (int pastedColIndex = 0; pastedColIndex < 100; ++pastedColIndex)
            //{
            //    Signs.Add(new SignData
            //    {
            //        Title = $"Jones #{pastedColIndex}",
            //        SvgSignTemplate = templateStorageItem.SvgSignTemplate,
            //        Fields = new Dictionary<string, string>
            //        {
            //            { "Name", $"Jones{pastedColIndex}" }
            //        }
            //    });
            //}

        }



        private async void MainGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Check if Ctrl is pressed and the key is 'V'
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(
                Windows.System.VirtualKey.Control);
            bool isCtrlDown = ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (isCtrlDown && e.Key == Windows.System.VirtualKey.V)
            {
                await PasteFromClipboardAsync();
                e.Handled = true; // Prevent further handling
            }

        }

        private void MainGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var menuFlyout = new MenuFlyout();

            var addSignTemplateItem = new MenuFlyoutItem { Text = "Add New Sign" };
            addSignTemplateItem.Click += (s, args) => ShowMessage("add selected");

            var pasteItem = new MenuFlyoutItem { Text = "Paste Sign Data" };
            pasteItem.Click += async (s, args) =>
            {
                try
                {
                    await PasteFromClipboardAsync();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            };

            var separator = new MenuFlyoutSeparator();

            var item3 = new MenuFlyoutItem { Text = "Print Signs" };
            item3.Click += (s, args) => ShowMessage("print selected");

            // Enable/disable Paste based on clipboard content
            var dataView = Clipboard.GetContent();
            pasteItem.IsEnabled = dataView.Contains(StandardDataFormats.Text);

            menuFlyout.Items.Add(addSignTemplateItem);
            menuFlyout.Items.Add(pasteItem);
            menuFlyout.Items.Add(separator);
            menuFlyout.Items.Add(item3);

            menuFlyout.ShowAt(sender as FrameworkElement, e.GetPosition(sender as FrameworkElement));
        }

        private async void ShowMessage(string message)
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
                            ShowMessage( $"Accepted: {file.Name}");
                        }
                        else
                        {
                            ShowMessage($"Rejected: {file.Name} (not SVG)");
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
    }
}