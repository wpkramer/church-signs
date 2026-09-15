using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace ChurchSigns.UI.Controls
{   
    /// <summary>
    /// Responsible for presenting a grid of a sign template
    /// mapped to pasted data and notifying clients of our
    /// records and fields
    /// </summary>
    public sealed partial class TemplateMappingView : UserControl
    {
        private readonly List< Dictionary<string, string> > _records;

        public TemplateMappingView()
        {
            _records = new List< Dictionary<string, string> >();
            InitializeComponent();
        }

        public delegate void MultiSignDataHandler( IReadOnlyList< Dictionary<string, string> > data );
        public event MultiSignDataHandler? MultiSignDataChanged;

        public static readonly DependencyProperty SelectedTemplateProperty =
            DependencyProperty.Register(
                nameof(SelectedTemplate),
                typeof(SignTemplate),
                typeof(TemplateMappingView),
                new PropertyMetadata(null, OnTemplateChanged));

        public static readonly DependencyProperty PastedDataProperty =
            DependencyProperty.Register(
                nameof(PastedData),
                typeof(PastedRecordData),
                typeof(TemplateMappingView),
                new PropertyMetadata(new PastedRecordData(string.Empty), OnPasteDataChanged));



        private static void OnPasteDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TemplateMappingView control)
            {
                control.Rebuild();
            }
        }

        private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TemplateMappingView control)
            {
                control.Rebuild();
            }
        }

 

        public PastedRecordData PastedData
        {
            get => (PastedRecordData)GetValue(PastedDataProperty);
            set => SetValue(PastedDataProperty, value);
        }

        public SignTemplate SelectedTemplate
        {
            get => (SignTemplate)GetValue(SelectedTemplateProperty);
            set => SetValue(SelectedTemplateProperty, value);
        }

        public IReadOnlyList<Dictionary<string, string>> Records => _records;


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

            if (col < 0 || col >= TemplateMappingGrid.ColumnDefinitions.Count)
                throw new ArgumentOutOfRangeException(nameof(col));
            if (row < 0 || row >= TemplateMappingGrid.RowDefinitions.Count)
                throw new ArgumentOutOfRangeException(nameof(row));
            Grid.SetColumn(ctrlToAdd, col);
            Grid.SetRow(ctrlToAdd, row);
            TemplateMappingGrid.Children.Add(ctrlToAdd);
        }

        private void Rebuild()
        {
            if (SelectedTemplate == null)
            {
                Debug.WriteLine("Rebuiding mapping grid without a selected template!");
                _signTemplateDataMap = null;
                return;
            }

            switch (SelectedTemplate.SignMode)
            {
                case TemplateSignMode.MultiSign:
                    GenerateMultiSignGrid();
                    break;
                case TemplateSignMode.SingleSign:
                    GenerateSingleSign();
                    break;
                default:
                    throw new ApplicationException($"Unexpected SignMode {SelectedTemplate.SignMode}");
            }
            

        }

        private void GenerateSingleSign()
        {
            TemplateMappingGrid.Children.Clear();
            TemplateMappingGrid.RowDefinitions.Clear();
            TemplateMappingGrid.ColumnDefinitions.Clear();


        }

        private void GenerateMultiSignGrid()
        {
            TemplateMappingGrid.Children.Clear();
            TemplateMappingGrid.RowDefinitions.Clear();
            TemplateMappingGrid.ColumnDefinitions.Clear();

            SignTemplate signTemplate = SelectedTemplate;

            // mapping rows
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            // data rows
            if (PastedData.ColumnHeaderNames.Count > 0)
            {
                for (int i = 0; i < PastedData.Records.Count; i++)
                {
                    TemplateMappingGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                }
            }


            int columnCount = signTemplate.FieldNames.Count;
            columnCount = Math.Max(1, columnCount);


            columnCount = Math.Max(columnCount, PastedData.ColumnHeaderNames.Count);



            for (int i = 0; i < columnCount; i++)
            {
                TemplateMappingGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }



            Thickness columnSeperation = new Thickness(2d);
            object objColHdrStyle = Application.Current.Resources["BodyStrongTextBlockStyle"];




            // if no pasted data we are just going to show
            // our field names
            if (PastedData.ColumnHeaderNames.Count == 0)
            {
                _signTemplateDataMap = null;
                for (int i = 0; i < columnCount; ++i)
                {
                    if (i == signTemplate.FieldNames.Count)
                        break;

                    TextBlock textBlock = new TextBlock
                    {
                        Text = SelectedTemplate.FieldNames[i],
                        Margin = columnSeperation
                    };
                    if (objColHdrStyle is Style style)
                    {
                        textBlock.Style = style;
                    }
                    // add control the column head row
                    AddControl(i, 0, textBlock);
                }
                BuildRecords();
                return; // we're done
            }


            _signTemplateDataMap = new SignTemplateDataMap(signTemplate, PastedData);
            columnCount = PastedData.ColumnHeaderNames.Count;

            for (int i = 0; i < columnCount; ++i)
            {

                TextBlock textBlock = new TextBlock
                {
                    Text = PastedData.ColumnHeaderNames[i],
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
                            // commented out this logic to stop a double refresh
                            // when a combBox replaces another combo box, todo: review after testing
                            //bool showSigns = true;
                            if (affectedComboIndex >= 0 && affectedComboIndex < _fieldSelectionBoxes.Count)
                            {
                                _fieldSelectionBoxes[affectedComboIndex].SelectedIndex = 0;

                                //    showSigns = false; // we'll show them on the next event handler
                            }
                            //if (showSigns)
                            //{
                            BuildRecords();
                            //}
                        }

                    }
                };

                AddControl(pastedColIndex, 1, combo);
            }

            int rowNumber = 1;
            // Paste in the data rows
            foreach (var rowData in PastedData.Records)
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

            BuildRecords();
        }

        private void BuildRecords()
        {
            _records.Clear();

            // add a record for an empty sign template
            if (_signTemplateDataMap == null)
            {
                _records.Add(new Dictionary<string, string>());
            }
            else
            {

                foreach (var record in _signTemplateDataMap.CreateMappedRecords())
                {
                    _records.Add(record);
                }
            }

            if (MultiSignDataChanged != null)
            {
                MultiSignDataChanged(Records);
            }

        }
    }
}
