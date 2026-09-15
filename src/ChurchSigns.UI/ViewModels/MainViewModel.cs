using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using Microsoft.UI.Xaml;
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
        private PastedRecordData _lastTablePaste = new PastedRecordData();
        private PastedFieldPairs _lastFieldPairs = new PastedFieldPairs();

        public ObservableCollection<SignTemplate> Templates { get; } = [];
        public ObservableCollection<ChurchSign> Signs { get; } = [];
        public ObservableCollection<GroupInfoList>? GroupedTemplates { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PastedRecordData LastTablePaste
        {
            get => _lastTablePaste;
            set
            {
                if(value != _lastTablePaste)
                {
                    _lastTablePaste = value;
                    OnPropertyChanged(nameof(LastTablePaste));
                }
            }
        }

        public PastedFieldPairs LastFieldPairs
        {
            get => _lastFieldPairs;
            set
            {
                if (value != _lastFieldPairs)
                {
                    _lastFieldPairs = value;
                    OnPropertyChanged(nameof(LastFieldPairs));
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
                LastTablePaste = new PastedRecordData();
                LastFieldPairs = new PastedFieldPairs();
                OnPropertyChanged(nameof(IsCustomSelected));
                OnPropertyChanged(nameof(SelectedTemplate));
                OnPropertyChanged(nameof(MultiSignVisibility));
                OnPropertyChanged(nameof(SingleSignVisibility));

            }
        }

        public Visibility MultiSignVisibility
        {
            get
            {
                if (SelectedTemplate == null)
                    return Visibility.Collapsed;
                if (SelectedTemplate.SignMode == TemplateSignMode.SingleSign)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        public Visibility SingleSignVisibility
        {
            get
            {
                if (SelectedTemplate == null)
                    return Visibility.Collapsed;
                if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
                    return Visibility.Collapsed;
                return Visibility.Visible;
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
            if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
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
                    var sign = new ChurchSign(SelectedTemplate, row);

                    Signs.Add(sign);
                }
                if (Signs.Count == 0)
                    Signs.Add(SelectedTemplate.CreatePlaceholderSign());
            }
        }

        public void ReplaceSignsFromFieldData(IReadOnlyDictionary<string, string> data)
        {
            if (SelectedTemplate.SignMode == TemplateSignMode.SingleSign)
            {
                Signs.Clear();

                Signs.Add( new ChurchSign(SelectedTemplate, data));

            }
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
            if (SelectedTemplate.SignMode == TemplateSignMode.MultiSign)
            {
                LastTablePaste = new PastedRecordData(clipboardText);
                LastFieldPairs = new PastedFieldPairs();
            }
            else
            {
                LastTablePaste = new PastedRecordData();
                LastFieldPairs = new PastedFieldPairs(clipboardText);
            }
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
