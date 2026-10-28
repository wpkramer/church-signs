using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
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


namespace ChurchSigns
{
    /// <summary>
    /// Church Signs is a one Window / Page Appliction
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();


            MappingView.MultiSignDataChanged += MappingView_MappedDataChangedAsync;
       //     PairsView.SingleSignDataChanged += PairsView_SingleSignDataChanged;


            Clipboard.ContentChanged += Clipboard_ContentChanged;
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
            

            this.Activated += MainWindow_Activated;
    //        ViewModel.Templates.CollectionChanged += Templates_CollectionChanged;
           // ViewModel.WindowId = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        }

       

        //private void Templates_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    TemplateSelector.RefreshGrouping();
        //}

        //private void PairsView_SingleSignDataChanged(IReadOnlyDictionary<string, string> data)
        //{
        //    // test removing this ViewModel.ReplaceSignsFromFieldData(data);
        //    //SignPrintSelector.ShowSigns();
        //    //SignPrintSelector.SelectAllSigns();
        //}

        private async void MappingView_MappedDataChangedAsync(IReadOnlyList<Dictionary<string, string>> data)
        {
            bool signsWereMapped = await ViewModel.ReplaceSignsFromMappedDataAsync(data);
            if (signsWereMapped)
            {
                SignPrintSelector.SelectAllSigns();
            }
            else
            {
                SignPrintSelector.SelectNoSigns();
            }

        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }

        private void Clipboard_ContentChanged(object? sender, object e)
        {
            HasPasteData = Clipboard.GetContent().Contains(StandardDataFormats.Text);
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.XamlRoot = this.Content.XamlRoot;
            ViewModel.MainWindow = this;
        }

        public async Task PrepareWindowAsync(nint hwnd)
        {

            await ViewModel.InitializeAsync(hwnd);
            TemplateSelector.ViewModel = ViewModel;
            SignPrintSelector.ViewModel = ViewModel;
            
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
                    //SignPrintSelector.SelectAllSigns();
                }
                catch (Exception ex) { await ViewModel.ShowMessageAsync(ex.Message); }
                e.Handled = true;
            }

        }

        public bool HasPasteData { get; private set; } = false;


        private async void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (HasPasteData)
            {
                try
                {
                    await ViewModel.PasteAsync();

                    //SignPrintSelector.SelectAllSigns();

                }
                catch (Exception ex)
                {
                    await ViewModel.ShowMessageAsync($"{ex.GetType().Name}: {ex.Message}");
                }

            }
        }

    }
}