using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using ChurchSigns.UI.Util;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
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
                if(tmplt.IsValid)
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
            //for(int i = 0; i < 100; ++i)
            //{
            //    Signs.Add(new SignData
            //    {
            //        Title = $"Jones #{i}",
            //        SvgSignTemplate = templateStorageItem.SvgSignTemplate,
            //        Fields = new Dictionary<string, string>
            //        {
            //            { "Name", $"Jones{i}" }
            //        }
            //    });
            //}

        }



        //private static async Task ReadContentFileAsync(SignData templateStorageItem)
        //{
        //    try
        //    {

        //        var svgFile = await StorageFile.GetFileFromApplicationUriAsync(
        //            new Uri("ms-appx:///Templates/AwanaSparx/LeaderSign.svg"));

        //        templateStorageItem.SvgSignTemplate = await FileIO.ReadTextAsync(svgFile);
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.WriteLine($"{ex.GetType().Name}: reading provided sign templateStorageItem {ex.Message}");
        //    }
        //}

        public async Task<ObservableCollection<GroupInfoList>> GetSignTemplatesGroupedAsync()
        {
            // Grab Contact objects from pre-existing list (list is returned from function GetContactsAsync())
            var query = from item in Templates
                        group item by item.Group into g
                        orderby g.Key
                        select new GroupInfoList(g) { Key = g.Key };

            return new ObservableCollection<GroupInfoList>(query);
        }


        public ObservableCollection<SignData> Signs { get; private set; }
        public ObservableCollection<SignTemplate> Templates { get; private set; }


        private void SignTemplatesListView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            
            LoadSigns(SignTemplatesListView.SelectedItem);
            ShowPasteArea(SignTemplatesListView.SelectedItem);
        }

        private void ShowPasteArea(object selectedItem)
        {
            
        }

        private void LoadSigns(object selectedItem)
        {
            if (selectedItem == null)
                return;
            
            if(selectedItem is SignTemplate signTemplate)
            {
                Signs.Clear();
                var sd = new SignData(signTemplate);
                sd.Fields.Add("Name", "Kramer");
                Signs.Add(sd);

                for(int i = 0; i < 100; i++)
                {
                    sd = new SignData(signTemplate);
                    sd.Fields.Add("Name", $"Jones #{i}");
                    Signs.Add(sd);
                }
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
            //for (int i = 0; i < 100; ++i)
            //{
            //    Signs.Add(new SignData
            //    {
            //        Title = $"Jones #{i}",
            //        SvgSignTemplate = templateStorageItem.SvgSignTemplate,
            //        Fields = new Dictionary<string, string>
            //        {
            //            { "Name", $"Jones{i}" }
            //        }
            //    });
            //}
        }
    }

   
}
