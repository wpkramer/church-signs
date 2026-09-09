using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ChurchSigns.UI.Services
{
    using ChurchSigns.UI.Helpers;
    using ChurchSigns.UI.Models;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.Foundation.Collections;
    using Windows.Storage;

    public sealed class TemplateStorageService
    {
        public static TemplateStorageService Instance { get; } = new();

        static string SidecarName(string svgFileName) =>
            Path.ChangeExtension(svgFileName, ".json"); // LeaderSign.svg → LeaderSign.json
       

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
                StorageFolder? categoryFolder = null;
                try
                {
                    categoryFolder = await templatesRoot.GetFolderAsync(category.ToString());
                }
                catch { continue; }

                foreach (var file in await categoryFolder.GetFilesAsync())
                {
                    if (!file.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var storageItem = new TemplateStorageItem
                    {
                        IsProvided = true,
                        SignCategory = category,
                        Filename = file.Name,
                        Content = await FileIO.ReadTextAsync(file)
                    };

                    string sidecarName = SidecarName(file.Name);
                    var sidecarFile = await categoryFolder.TryGetItemAsync(sidecarName) as StorageFile;
                    if(sidecarFile != null)
                    {
                        try
                        {
                            var sideCar = await LoadSidecarAsync(sidecarFile);
                            if(sideCar != null)
                            {
                                storageItem.SideCar = sideCar;
                            }

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


                    string sidecarName = SidecarName(file.Name);
                    var sidecarFile = await categoryFolder.TryGetItemAsync(sidecarName) as StorageFile;
                    if (sidecarFile != null)
                    {
                        try
                        {
                            var sideCar = await LoadSidecarAsync(sidecarFile);
                            storageItem.SideCar = sideCar;
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
                await SaveSidecarAsync(sidecarFile, item.SideCar);
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


        public async Task ExportSignTemplate(TemplateStorageItem storageItem, StorageFile file)
        {
            // Open the StorageFile for read/write and get a Stream to pass to ZipArchive
            var randomAccess = await file.OpenAsync(FileAccessMode.ReadWrite);
            using (var stream = randomAccess.AsStreamForWrite())
            using (var templateArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: false))
            {
                string tmpltFilename = storageItem.Filename;
                string sideCarFilename = SidecarName(tmpltFilename);

                AddZipEntry(storageItem.Content, templateArchive, tmpltFilename);
                string json = JsonSerializer.Serialize(storageItem.SideCar, SignJsonContext.Default.TemplateSidecar);
                AddZipEntry(json, templateArchive, sideCarFilename);
            }
        }

        public async Task<IReadOnlyList<TemplateStorageItem>> ImportSignTemplatesAsync(StorageFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            var extension = Path.GetExtension(file.Name);
            if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            {
                var single = await ImportSvgFileAsync(file);
                return single is null ? Array.Empty<TemplateStorageItem>() : new[] { single };
            }

            if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                return await ImportZipTemplatesAsync(file);

            return Array.Empty<TemplateStorageItem>();
        }

        private async Task<TemplateStorageItem?> ImportSvgFileAsync(StorageFile file)
        {
            var content = await FileIO.ReadTextAsync(file);
            var sideCar = new TemplateSidecar();

            // Optional: load sibling .json if you support that for non-zip import
            // var sidecarName = SidecarName(file.Name);

            return new TemplateStorageItem
            {
                IsProvided = false,
                SignCategory = default,
                Filename = file.Name,
                Content = content,
                SideCar = sideCar
            };
        }

        private async Task<IReadOnlyList<TemplateStorageItem>> ImportZipTemplatesAsync(StorageFile file)
        {
            var results = new List<TemplateStorageItem>();

            using var randomAccess = await file.OpenAsync(FileAccessMode.Read);
            using var stream = randomAccess.AsStreamForRead();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            static string EntryFileName(ZipArchiveEntry e) =>
                Path.GetFileName(e.FullName.Replace('\\', '/'));

            static bool IsSvg(ZipArchiveEntry e)
            {
                var name = EntryFileName(e);
                return !string.IsNullOrEmpty(name)
                    && name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
            }

            // Index sidecar entries by file name (case-insensitive)
            var sidecarByName = archive.Entries
                .Select(e => (Entry: e, Name: EntryFileName(e)))
                .Where(x => !string.IsNullOrEmpty(x.Name)
                            && x.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Entry, StringComparer.OrdinalIgnoreCase);

            foreach (var signEntry in archive.Entries.Where(IsSvg))
            {
                var svgFileName = EntryFileName(signEntry);
                if (string.IsNullOrEmpty(svgFileName))
                    continue;

                var sidecarFileName = SidecarName(svgFileName); // LeaderSign.svg → LeaderSign.json

                TemplateSidecar sideCar;
                if (!sidecarByName.TryGetValue(sidecarFileName, out var sideCarEntry))
                {
                    Debug.WriteLine($"Sidecar for {svgFileName} not found");
                    sideCar = new TemplateSidecar();
                }
                else
                {
                    using var sideCarStream = sideCarEntry.Open();
                    using var reader = new StreamReader(sideCarStream);
                    var json = await reader.ReadToEndAsync();
                    sideCar = JsonSerializer.Deserialize(json, SignJsonContext.Default.TemplateSidecar)
                              ?? new TemplateSidecar();
                }

                using var signStream = signEntry.Open();
                using var signReader = new StreamReader(signStream);
                var content = await signReader.ReadToEndAsync();

                results.Add(new TemplateStorageItem
                {
                    IsProvided = false,
                    SignCategory = default, // or from sidecar later
                    Filename = svgFileName,
                    Content = content,
                    SideCar = sideCar
                });
            }

            return results;
        }





        private static void AddZipEntry(string content, ZipArchive templateArchive, string filename)
        {
            var zipEntry = templateArchive.CreateEntry(filename);
            using (var zipEntryStream = zipEntry.Open())
            {
                using (TextWriter writer = new StreamWriter(zipEntryStream))
                {
                    writer.Write(content);
                }
            }
        }


        private static async Task<TemplateSidecar> LoadSidecarAsync(StorageFile file)
        {
            var json = await FileIO.ReadTextAsync(file);
            return JsonSerializer.Deserialize<TemplateSidecar>(json, SignJsonContext.Default.TemplateSidecar)
                ?? new TemplateSidecar();
        }

        private static async Task SaveSidecarAsync(StorageFile file, TemplateSidecar sideCar)
        {
            string json = JsonSerializer.Serialize(sideCar,SignJsonContext.Default.TemplateSidecar);
            await FileIO.WriteTextAsync(file, json);
        }


    }
}
