using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
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
using Windows.Storage;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ChurchSigns.UserControls
{
    public sealed partial class SignPrintUserControl : UserControl
    {
        public SignPrintUserControl()
        {
            InitializeComponent();
        }

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(MainViewModel),
                typeof(SignPrintUserControl),
                new PropertyMetadata(null, OnViewModelChanged));

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var control = (SignPrintUserControl)d;

            //if (e.OldValue is MainViewModel oldVm)
            //    oldVm.PropertyChanged -= control.ViewModel_PropertyChanged;

            //if (e.NewValue is MainViewModel newVm)
            //{
            //    newVm.PropertyChanged += control.ViewModel_PropertyChanged;
            //    // Refresh view model dependent data for control
            //}
        }


        //private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    // put this here just to watch in debugger
        //    if (e.PropertyName == nameof(ViewModel.Signs))
        //    {
        //        // show our signs

        //    }
        //}


        public bool HasSelectedSigns
        {
            get
            {
                return SignGridView.SelectedItems.Count > 0;
            }
        }

        public void SelectAllSigns()
        {
            AllSignsCheckBox.IsChecked = false;
            AllSignsCheckBox.IsChecked = true;
        }

        public void SelectNoSigns()
        {
            AllSignsCheckBox.IsChecked = true;
            AllSignsCheckBox.IsChecked = false;
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

        private void AllSignsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectAllSigns(true);
        }



        private void AllSignsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectAllSigns(false);
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
            if (signs.Count > 0 && ViewModel != null)
            {
                await ViewModel.ExportPdfSignsAsync(signs);
            }

        }

        // TODO: review if this is required
        private void SignGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePrintPdfEnabled();
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<ChurchSign> signs = SignGridView.SelectedItems.OfType<ChurchSign>().ToList();
            if (signs.Count > 0 && ViewModel != null)
            {
                PrintButton.IsEnabled = false;
                PrintCanvas.Children.Clear();

                try
                {
                    await ViewModel.PrintSignsAsync(signs, PrintCanvas);
                }
                finally
                {
                    PrintCanvas.Children.Clear();
                    PrintButton.IsEnabled = true;
                }


            }
        }


    }
}
