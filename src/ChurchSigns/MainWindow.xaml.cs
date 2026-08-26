using ChurchSigns.UI.Models;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

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
            
            Signs = [];
            
        }


        /// <summary>
        /// Called from the App to load our templates.
        /// </summary>
        /// <returns></returns>
        public async Task InitializeTemplatesAsync()
        {
            SignData template = new()
            {
                Title = "Sign Template"
            };
            
            await ReadContentFileAsync(template);

            Signs.Add(template);

            Signs.Add(new SignData
            {
                Title = "William Kramer",
                Template = template.Template,
                Fields = new Dictionary<string, string>
                {
                    { "Name", "William Kramer" }
                }
            });
            Signs.Add(new SignData
            {
                Title = "John Smith",
                Template = template.Template,
                Fields = new Dictionary<string, string>
                {
                    { "Name", "Smith" }
                }
            });

        }

        

        private static async Task ReadContentFileAsync(SignData template)
        {
            try
            {
              
                
                var svgFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Templates/AwanaSparx/LeaderSign.svg"));

                template.Template = await FileIO.ReadTextAsync(svgFile);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{ex.GetType().Name}: reading provided sign template {ex.Message}");
            }
        }

        public ObservableCollection<SignData> Signs { get; private set; }


    }
}
