using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.UI.Services
{
    using ChurchSigns.UI.Models;
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

                    results.Add(new TemplateStorageItem
                    {
                        IsProvided = true,
                        SignCategory = category,
                        Filename = file.Name,
                        Content = await FileIO.ReadTextAsync(file)
                    });
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

                    results.Add(new TemplateStorageItem
                    {
                        IsProvided = false,
                        SignCategory = category,
                        Filename = file.Name,
                        Content = await FileIO.ReadTextAsync(file)
                    });
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
                await FileIO.WriteTextAsync(file, item.Content ?? string.Empty);
                item.IsProvided = false;
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
        }

        /// <summary>
        /// Package URI helper if you still prefer ms-appx paths for known content files.
        /// </summary>
        //public static Uri GetContentUri(SignCategory category, string filename) =>
        //    new($"ms-appx:///{TemplatesRoot}/{category}/{filename}");
    }
}
