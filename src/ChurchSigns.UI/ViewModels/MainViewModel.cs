using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ChurchSigns.UI.ViewModels
{
    public partial class MainViewModel(IClipboardService? clipboard = null) : INotifyPropertyChanged
    {
        private readonly IClipboardService _clipboard = clipboard ?? new WindowsClipboardService();
        private SignTemplate _selectedTemplate = CreateBlankTemplate();
        private PastedRecordData _lastPaste = new PastedRecordData();

        public ObservableCollection<SignTemplate> Templates { get; } = [];
        public ObservableCollection<ChurchSign> Signs { get; } = [];
        public ObservableCollection<GroupInfoList>? GroupedTemplates { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PastedRecordData LastPaste
        {
            get => _lastPaste;
            set
            {
                if(value != _lastPaste)
                {
                    _lastPaste = value;
                    OnPropertyChanged(nameof(LastPaste));
                }
            }
        }

        public SignTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (ReferenceEquals(_selectedTemplate, value) || value is null)
                    return;

                _selectedTemplate = value;
                // clear out the pasted data for a different template
                LastPaste = new PastedRecordData();
                OnPropertyChanged(nameof(IsCustomSelected));
                OnPropertyChanged(nameof(SelectedTemplate));


            }
        }


        public bool IsCustomSelected
        {
            get
            {
                if (_selectedTemplate != null)
                {

                    return !_selectedTemplate.IsProvided;
                }
                return false;
            }
        }

        public async Task InitializeAsync()
        {
            await TemplateStorageService.Instance.EnsureLocalFolderStructureAsync();

            Templates.Clear();
            foreach (var item in await TemplateStorageService.Instance.GetContentTemplatesAsync())
                TryAddTemplate(item);
            foreach (var item in await TemplateStorageService.Instance.GetLocalTemplatesAsync())
                TryAddTemplate(item);

            var grouped =
                from item in Templates
                group item by item.Group into g
                orderby g.Key
                select new GroupInfoList(g.Key, g);

            GroupedTemplates = new ObservableCollection<GroupInfoList>(grouped);

            OnPropertyChanged(nameof(GroupedTemplates));

            if (Templates.Count > 0)
                SelectedTemplate = Templates[0];
        }

        public void ReplaceSignsFromMappedData(IReadOnlyList<Dictionary<string, string>> data)
        {
            Signs.Clear();
            if (data.Count == 0 || (data.Count == 1 && data[0].Count == 0))
            {
                Signs.Add(SelectedTemplate.CreatePlaceholderSign());
                return;
            }
            foreach (var row in data)
            {
                if (row is null || row.Count == 0) continue;
                Signs.Add(new ChurchSign(SelectedTemplate) { Fields = row });
            }
            if (Signs.Count == 0)
                Signs.Add(SelectedTemplate.CreatePlaceholderSign());
        }


        public SignTemplate? AddTemplates(IEnumerable<TemplateStorageItem> storageItems)
        {
            SignTemplate? lastAdded = null;
            List<SignTemplate> templates = [];
            foreach (var storageItem in storageItems)
            {
                var template = new SignTemplate(storageItem);
                if (!template.IsValid)
                    continue;
                
                templates.Add(template);
                lastAdded = template;
            }
            UpsertTemplates(templates);
            return lastAdded;
        }

        private void UpsertTemplates(IEnumerable<SignTemplate> incoming)
        {
            // TODO: don't affect provided
            foreach (var template in incoming.Where(t => t.IsValid))
            {
                var existingIndex = IndexOfTemplate(template.Category, template.Filename);
                if (existingIndex >= 0)
                    Templates[existingIndex] = template; // replace
                else
                    Templates.Add(template);
            }

            RebuildGroupedTemplates();
        }

        private int IndexOfTemplate(SignCategory category, string filename)
        {
            for (int i = 0; i < Templates.Count; i++)
            {
                var t = Templates[i];
                if (!t.IsProvided && t.Category == category
                    && string.Equals(t.Filename, filename, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public void RebuildGroupedTemplates()
        {
            GroupedTemplates = new ObservableCollection<GroupInfoList>(
                from t in Templates
                group t by t.Group into g
                orderby g.Key
                select new GroupInfoList(g.Key, g));

            OnPropertyChanged(nameof(GroupedTemplates));
        }

        public async Task PasteAsync()
        {
            var text = await _clipboard.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Clipboard does not contain text for signs.");

            ApplyPaste(text);
        }

        private void ApplyPaste(string clipboardText)
        {
            LastPaste = new PastedRecordData(clipboardText);
        }

        private void TryAddTemplate(TemplateStorageItem item)
        {
            var t = new SignTemplate(item);
            if (t.IsValid)
                Templates.Add(t);
        }

        private static SignTemplate CreateBlankTemplate() =>
            new(new TemplateStorageItem
            {
                Content = """
                    <?xml version="1.0" encoding="UTF-8"?>
                    <svg xmlns="http://www.w3.org/2000/svg" width="100" height="100"></svg>
                    """,
                IsProvided = true,
                SignCategory = SignCategory.Miscellaneous,
                Filename = "No Template Selected.svg",
            });

        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
