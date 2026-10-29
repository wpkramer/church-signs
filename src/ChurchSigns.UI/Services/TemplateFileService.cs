using ChurchSigns.UI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Windows.ApplicationModel;


namespace ChurchSigns.UI.Services
{
    public sealed class TemplateFileService
    {

        public static TemplateFileService Instance { get; } = new();

        private const string _templateRootName = "Templates";
        private const string _jsonExtension = ".json";
        private const string _svgExtension = ".svg";
        private const string _zipExtension = ".zip";
        private string _providedTemplatesRoot;
        private string _userTemplatesRoot;
        private TemplateFileService()
        {
            _providedTemplatesRoot = Path.Combine(Package.Current.InstalledLocation.Path, _templateRootName);
            _userTemplatesRoot = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, _templateRootName);

            // establish application folder structure
            if(!Directory.Exists(_providedTemplatesRoot))
            {
                throw new ApplicationException("Installation problem, provided templates are missing!");
            }

            if (!Directory.Exists(_userTemplatesRoot))
            {
                _ = Directory.CreateDirectory(_userTemplatesRoot);
            }

            foreach (SignCategory category in Enum.GetValues<SignCategory>())
            {
                _ = Directory.CreateDirectory(Path.Combine(_userTemplatesRoot, category.ToString()));
            }

        }

        public IReadOnlyList<TemplateStorageItem> GetProvidedTemplates()
        {
            return GetTemplates(true, _providedTemplatesRoot);
        }

        public IReadOnlyList<TemplateStorageItem> GetUserTemplates()
        {
            return GetTemplates(false, _userTemplatesRoot);
        }

        /// <summary>
        /// Save user sign templates
        /// </summary>
        public void Save(TemplateStorageItem item)
        {
            item.IsProvided = false; 
            string filePath = Path.Combine(_userTemplatesRoot, item.SignCategory.ToString(), item.Filename);
            _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, item.Content ?? string.Empty);

            string sidecarPath = Path.ChangeExtension(filePath, _jsonExtension);
            string json = item.SideCar.ToJson();
            File.WriteAllText(sidecarPath, json);
        }

        public void Delete(TemplateStorageItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.IsProvided)
                return;

            string filePath = Path.Combine(_userTemplatesRoot, item.SignCategory.ToString(), item.Filename);
            string sidecarPath = Path.ChangeExtension(filePath, _jsonExtension);

            if (File.Exists(filePath))
                File.Delete(filePath);
            if (File.Exists(sidecarPath))
                File.Delete(sidecarPath);
        }

        /// <summary>
        /// Export from our sign archive to the file system
        /// </summary>
        public static void Export(TemplateStorageItem storageItem, string filePath)
        {
            using(FileStream fstream = File.Create(filePath))
            using (var templateArchive = new ZipArchive(fstream, ZipArchiveMode.Create, false))
            {
                AddZipEntry(storageItem.Content, templateArchive, storageItem.Filename);
                string sideCarFilename = Path.ChangeExtension(storageItem.Filename, _jsonExtension);
                string sideCarJson = storageItem.SideCar.ToJson();
                AddZipEntry(sideCarJson, templateArchive, sideCarFilename);
            }
        }

        /// <summary>
        /// Read either a svg file or a collection of svg files
        /// from a zip archive.
        /// </summary>
        public static IReadOnlyList<TemplateStorageItem> Read(string filePath)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(filePath);
 
            using FileStream fstreamToImport = File.OpenRead(filePath);

            string extension = Path.GetExtension(filePath);
            if(extension.Equals(_svgExtension, StringComparison.OrdinalIgnoreCase))
            {
                string filename = Path.GetFileName(filePath);
                TemplateStorageItem? oneTemplate = ReadSvg(fstreamToImport, filename);
                return oneTemplate is null ? Array.Empty<TemplateStorageItem>() : new[] { oneTemplate };
            }

            if(extension.Equals(_zipExtension, StringComparison.OrdinalIgnoreCase))
            {
                return ReadZip(fstreamToImport);
            }

            // tried to import something else, ignore it
            return Array.Empty<TemplateStorageItem>();
        }

        /// <summary>
        /// Read the zip file and extract svg sign templates
        /// and any assocated sidecar files
        /// </summary>
        private static IReadOnlyList<TemplateStorageItem> ReadZip(FileStream fstreamToImport)
        {
            var results = new List<TemplateStorageItem>();


            using var archive = new ZipArchive(fstreamToImport, ZipArchiveMode.Read, leaveOpen: false);

            static string EntryFileName(ZipArchiveEntry e) =>
                Path.GetFileName(e.FullName.Replace('\\', '/'));

            static bool IsSvg(ZipArchiveEntry e)
            {
                var name = EntryFileName(e);
                return !string.IsNullOrEmpty(name)
                    && name.EndsWith(_svgExtension, StringComparison.OrdinalIgnoreCase);
            }

            // Index sidecar entries by file name (case-insensitive)
            var sidecarByName = archive.Entries
                .Select(e => (Entry: e, Name: EntryFileName(e)))
                .Where(x => !string.IsNullOrEmpty(x.Name)
                            && x.Name.EndsWith(_jsonExtension, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Entry, StringComparer.OrdinalIgnoreCase);

            foreach (var signEntry in archive.Entries.Where(IsSvg))
            {
                var svgFileName = EntryFileName(signEntry);
                if (string.IsNullOrEmpty(svgFileName))
                    continue;

                var sidecarFileName = Path.ChangeExtension(svgFileName, _jsonExtension);

                TemplateSidecar sideCar;
                string content;
                if (!sidecarByName.TryGetValue(sidecarFileName, out var sideCarEntry))
                {
                    Debug.WriteLine($"Sidecar for {svgFileName} not found");
                    sideCar = new TemplateSidecar();
                }
                else
                {
                    using (Stream sideCarStream = sideCarEntry.Open())
                    {
                        using (TextReader reader = new StreamReader(sideCarStream))
                        {
                            var json = reader.ReadToEnd();
                            sideCar = TemplateSidecar.CreateFromJsonText(json);
                        }
                    }

                }

                using (var signStream = signEntry.Open())
                using(TextReader reader = new StreamReader(signStream))
                {
                    content = reader.ReadToEnd();
                }
  
                results.Add(new TemplateStorageItem
                {
                    IsProvided = false,
                    SignCategory = default,
                    Filename = svgFileName,
                    Content = content,
                    SideCar = sideCar
                });
            }

            return results;
        }

        /// <summary>
        /// Create a storage item from a SVG filestream
        /// </summary>
        private static TemplateStorageItem? ReadSvg(FileStream fstreamToImport, string filename)
        {
            using (TextReader reader = new StreamReader(fstreamToImport))
            {
                string content = reader.ReadToEnd();
                TemplateSidecar sideCar = new();

                return new TemplateStorageItem
                {
                    IsProvided = false,
                    SignCategory = default,
                    Filename = filename,
                    Content = content,
                    SideCar = sideCar
                };

            }
        }

        private static void AddZipEntry(string content, ZipArchive templateArchive, string filename)
        {
            var zipEntry = templateArchive.CreateEntry(filename);
            using (var zipEntryStream = zipEntry.Open())
            using (TextWriter writer = new StreamWriter(zipEntryStream))
            {
                writer.Write(content);
            }
        }

        private (string svgContent,TemplateSidecar sideCar) ReadContentAndSideCar(string svgFilePath)
        {
            string content = string.Empty;
            TemplateSidecar sideCar = new TemplateSidecar();

            content = File.ReadAllText(svgFilePath);

            try
            {
                string sidecarPath = Path.ChangeExtension(svgFilePath, _jsonExtension);
                if (File.Exists(sidecarPath))
                {
                    string json = File.ReadAllText(sidecarPath);
                    sideCar = TemplateSidecar.CreateFromJsonText(json);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"{ex.GetType().Name} reading sidecar");
                // ignore sidecar error and just leave it blank
            }
            return (content, sideCar);
        }

        /// <summary>
        /// Read all the sign templates starting from the root template directory
        /// </summary>
        private IReadOnlyList<TemplateStorageItem> GetTemplates(bool isProvided, string templatesRoot)
        {
            var results = new List<TemplateStorageItem>();

                foreach(SignCategory category in Enum.GetValues<SignCategory>())
                {
                    string categoryPath = Path.Combine(templatesRoot, category.ToString());
                    if(Directory.Exists(categoryPath))
                    {
                    // Sign templates are svg files and may also have a sidecar json file
                    foreach (var filepath in Directory.EnumerateFiles(categoryPath, "*" + _svgExtension))
                    {
                        var contentAndSidecar = ReadContentAndSideCar(filepath);
                        var storageItem = new TemplateStorageItem
                        {
                            IsProvided = isProvided,
                            SignCategory = category,
                            Filename = Path.GetFileName(filepath),
                            Content = contentAndSidecar.svgContent,
                            SideCar = contentAndSidecar.sideCar
                        };

                        results.Add(storageItem);

                    }                        
                    
                }
            }

            return results;
        }

    }
}
