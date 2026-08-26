using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SignLib.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
            Signs = new ObservableCollection<MergedSign>();
            // string svgContent = File.ReadAllText(".\\Templates\\AwanaSparx\\LeaderSign.svg");
            // MergedSign sign = SignService.GenerateTemplateImage(svgContent);
            //  ImageSource imageSource = new ImageSource(".\\Templates\\AwanaSparx\\LeaderSign.svg");

            MergedSign template = new MergedSign();
            template.TemplatePath = "ms-appx:///Templates/AwanaSparx/LeaderSign.svg";
            template.Title = "Leader Sign";
            //template.Source = new SvgImageSource(new Uri("ms-appx:///Templates/AwanaSparx/LeaderSign.svg"));
           // _ = LoadSvgAsync(template) ;
            
            Signs.Add(template);
            Signs.Add(new MergedSign
            { TemplatePath = template.TemplatePath ,
            Title = "William Kramer",
            MergeValues = new Dictionary<string, string>
            {
                { "{{Name}}", "Kramer" }
            }
            });
            Signs.Add(new MergedSign
            {
                TemplatePath = template.TemplatePath,
                Title = "John Smith",
                MergeValues = new Dictionary<string, string>
            {
                { "{{Name}}", "Smith" }
            }
            });
            // set FlipView's items from code-behind to avoid XAML compile-time type mismatch
            SignFlipView.ItemsSource = Signs;

            

            // svgImageSource.SetSourceAsync(streamSource);
        }

        private async Task LoadSvgForSignAsync(WebView2 webView, MergedSign sign)
        {
            try
            {
                // Load SVG template
                StorageFile svgFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(sign.TemplatePath));
                string svgContent = await FileIO.ReadTextAsync(svgFile);

                // Replace placeholders
                foreach (var kvp in sign.MergeValues)
                {
                    svgContent = svgContent.Replace(kvp.Key, kvp.Value);
                }

                // Wrap in HTML for WebView2
                string htmlWrapper = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
    body {{
        margin: 0;
        padding: 0;
        background-color: transparent;
    }}
    svg {{
        width: 100%;
        height: 100%;
    }}
</style>
</head>
<body>
{svgContent}
</body>
</html>";

                await webView.EnsureCoreWebView2Async();
                webView.NavigateToString(htmlWrapper);
            }
            catch (Exception ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to load SVG: {ex.Message}",
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();
            }
        }

        public ObservableCollection<MergedSign> Signs { get; private set; }

        private void SvgWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (!args.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"SVG load failed: {args.WebErrorStatus}");
            }
        }

        private async void SvgWebView_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is WebView2 webView && webView.DataContext is MergedSign sign)
            {
                await LoadSvgForSignAsync(webView, sign);
            }
        }
    }
}
