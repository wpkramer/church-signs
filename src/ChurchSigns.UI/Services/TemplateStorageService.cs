using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ChurchSigns.UI.Services
{
    using ChurchSigns.UI.Helpers;
    using ChurchSigns.UI.Models;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;

    public sealed class TemplateStorageService
    {
        public static TemplateStorageService Instance { get; } = new();

        private const string TemplatesRoot = "Templates";
        private TemplateStorageService() { }

        public async Task EnsureLocalFolderStructureAsync()
        {
            var root = await ApplicationData.Current.LocalFolder
                .CreateFolderAsync(TemplatesRoot, CreationCollisionOption.OpenIfExists);

            foreach (SignCategory category in Enum.GetValues<SignCategory>())
            {
                await root.CreateFolderAsync(category.ToString(), CreationCollisionOption.OpenIfExists);
            }
        }

        public async Task<IReadOnlyList<TemplateStorageItem>> GetContentTemplatesAsync()
        {
            var results = new List<TemplateStorageItem>();
            StorageFolder templatesRoot;

            try
            {
                templatesRoot = await StorageFolder.GetFolderFromPathAsync(
                    // Prefer installed location for package content:
                    // Package.Current.InstalledLocation + "\Templates"
                    System.IO.Path.Combine(
                        Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                        TemplatesRoot));
            }
            catch
            {
                return (IReadOnlyList<TemplateStorageItem>)results;
            }

            foreach (SignCategory category in Enum.GetValues<SignCategory>())
            {
                StorageFolder categoryFolder = null;
                try
                {
                    categoryFolder = await templatesRoot.GetFolderAsync(category.ToString());
                }
                catch { continue; }

                foreach (var file in await categoryFolder.GetFilesAsync())
                {
                    if (!file.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var storageItem =new TemplateStorageItem
                    {
                        IsProvided = true,
                        SignCategory = category,
                        Filename = file.Name,
                        Content = await FileIO.ReadTextAsync(file)
                    };

                    string sidecarName = Path.ChangeExtension(file.Name, ".json");
                    var sidecarFile = await categoryFolder.TryGetItemAsync(sidecarName) as StorageFile;
                    if(sidecarFile != null)
                    {
                        try
                        {
                            var properties = await LoadAsync(sidecarFile);
                            storageItem.PreviewFields = properties;
                        }
                        catch
                        {
                            // Ignore sidecar load errors, just use default preview fields
                        }
                    }

                    results.Add(storageItem);
                    
                }
            }

            return (IReadOnlyList<TemplateStorageItem>)results;
        }

        public async Task<IReadOnlyList<TemplateStorageItem>> GetLocalTemplatesAsync()
        {
            await EnsureLocalFolderStructureAsync();
            var results = new List<TemplateStorageItem>();

            var root = await ApplicationData.Current.LocalFolder.GetFolderAsync(TemplatesRoot);

            foreach (SignCategory category in Enum.GetValues<SignCategory>())
            {
                var categoryFolder = await root.GetFolderAsync(category.ToString());
                foreach (var file in await categoryFolder.GetFilesAsync())
                {
                    if (!file.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var storageItem = new TemplateStorageItem
                    {
                        IsProvided = false,
                        SignCategory = category,
                        Filename = file.Name,
                        Content = await FileIO.ReadTextAsync(file)
                    };


                    string sidecarName = Path.ChangeExtension(file.Name, ".json");
                    var sidecarFile = await categoryFolder.TryGetItemAsync(sidecarName) as StorageFile;
                    if (sidecarFile != null)
                    {
                        try
                        {
                            var properties = await LoadAsync(sidecarFile);
                            storageItem.PreviewFields = properties;
                        }
                        catch
                        {
                            // Ignore sidecar load errors, just use default preview fields
                        }
                    }

                    results.Add(storageItem);
                }
            }

            return results;
        }

        public async Task SaveLocalAsync(TemplateStorageItem item, bool overwrite = false)
        {
            await EnsureLocalFolderStructureAsync();

            var root = await ApplicationData.Current.LocalFolder.GetFolderAsync(TemplatesRoot);
            var categoryFolder = await root.GetFolderAsync(item.SignCategory.ToString());

            var collision = overwrite
                ? CreationCollisionOption.ReplaceExisting
                : CreationCollisionOption.FailIfExists;

            try
            {
                var file = await categoryFolder.CreateFileAsync(item.Filename, collision); 
                item.IsProvided = false;

                await FileIO.WriteTextAsync(file, item.Content ?? string.Empty);

                string sidecarFileName = Path.ChangeExtension(item.Filename, ".json");
                var sidecarFile = await categoryFolder.CreateFileAsync(sidecarFileName, CreationCollisionOption.ReplaceExisting);
                await SaveAsync(sidecarFile, item.PreviewFields);
            }
            catch (Exception) when (!overwrite)
            {
                // File exists — caller can ask Replace vs Keep both (rename)
                throw;
            }
        }

        public async Task DeleteLocalAsync(SignCategory category, string filename)
        {
            var root = await ApplicationData.Current.LocalFolder.GetFolderAsync(TemplatesRoot);
            var categoryFolder = await root.GetFolderAsync(category.ToString());
            var file = await categoryFolder.GetFileAsync(filename);
            await file.DeleteAsync();
            var sidecarFileName = Path.ChangeExtension(filename, ".json");
            var sidecarFile = await categoryFolder.TryGetItemAsync(sidecarFileName) as StorageFile;
            if (sidecarFile != null)
            {
                await sidecarFile.DeleteAsync();
            }
        }


        private static async Task<SignTemplateProperties> LoadAsync(StorageFile file)
        {
            var json = await FileIO.ReadTextAsync(file);
            return JsonSerializer.Deserialize<SignTemplateProperties>(json, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<SignTemplateProperties>)SignJsonContext.WithOptions.SignTemplateProperties);
        }

        private static async Task SaveAsync(StorageFile file, SignTemplateProperties values)
        {
            var json = JsonSerializer.Serialize(values, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<SignTemplateProperties>)SignJsonContext.WithOptions.SignTemplateProperties);
            await FileIO.WriteTextAsync(file, json);
        }

        /// <summary>
        /// Package URI helper if you still prefer ms-appx paths for known content files.
        /// </summary>
        //public static Uri GetContentUri(SignCategory category, string filename) =>
        //    new($"ms-appx:///{TemplatesRoot}/{category}/{filename}");
    }
}
