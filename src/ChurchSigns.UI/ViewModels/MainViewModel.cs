using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using ChurchSigns.UI.Util;
using Microsoft.UI.Xaml;
using System;
using System.Collections;
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
        private PastedRecordData? _lastPaste;
        private SignTemplateDataMap? _dataMap;

        public ObservableCollection<SignTemplate> Templates { get; } = [];
        public ObservableCollection<ChurchSign> Signs { get; } = [];
        public ObservableCollection<GroupInfoList>? GroupedTemplates { get; private set; }

        public SignTemplateDataMap? DataMap => _dataMap;
        public PastedRecordData? LastPaste => _lastPaste;

        public event EventHandler? MappingReset;
        public event EventHandler? MappingUpdated;
        public event PropertyChangedEventHandler? PropertyChanged;

        public SignTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (ReferenceEquals(_selectedTemplate, value) || value is null)
                    return;

                _selectedTemplate = value;
                _lastPaste = null;
                _dataMap = null;
                Signs.Clear();

                //SignData sd = new SignData(_selectedTemplate);
                
                // One placeholder sign so the preview shows the template
                ChurchSign sd = _selectedTemplate.CreatePlaceholderSign();

                Signs.Add(sd);

                OnPropertyChanged();
         //       OnPropertyChanged(nameof(Signs));
                OnPropertyChanged(nameof(DataMap));
                OnPropertyChanged(nameof(IsCustomSelected));
                MappingReset?.Invoke(this, EventArgs.Empty);
                IsShowingPlaceholder = true;
            }
        }

        private bool _isShowingPlaceholder;
        public bool IsShowingPlaceholder
        {
            get
            {
                return _isShowingPlaceholder;
            }
            private set
            {
                if(_isShowingPlaceholder != value)
                {
                    _isShowingPlaceholder = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NewSignTemplateVisiblity));
                    OnPropertyChanged(nameof(SignsWithDataVisiblility));
                }
            }
        }


        public Visibility NewSignTemplateVisiblity
        {
            get
            {
                return IsShowingPlaceholder ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility SignsWithDataVisiblility
        {
            get
            {
                return IsShowingPlaceholder ? Visibility.Collapsed : Visibility.Visible;
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

        public SignTemplate? AddLocalTemplate(TemplateStorageItem item)
        {
            var template = new SignTemplate(item);
            if (!template.IsValid)
                return null;

            Templates.Add(template);
            RebuildGroupedTemplates();
            return template;
        }

        public SignTemplate? AddTemplates(IEnumerable<TemplateStorageItem> storageItems)
        {
            SignTemplate? lastAdded = null;
            List<SignTemplate> templates = new List<SignTemplate>();
            foreach(var storageItem in storageItems)
            {
                var template = new SignTemplate(storageItem);
                if (!template.IsValid)
                    continue;
                // TODO: if template name and category already exists then replace
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

        //public void RebuildGroupedTemplates()
        //{
        //    GroupedTemplates = new ObservableCollection<GroupInfoList>(
        //        from t in Templates
        //        group t by t.Group into g
        //        orderby g.Key
        //        select new GroupInfoList(g.Key, g));

        //    OnPropertyChanged(nameof(GroupedTemplates));
        //    GroupedTemplatesChanged?.Invoke(this, EventArgs.Empty);
        //}

        public event EventHandler? GroupedTemplatesChanged;

        public async Task PasteAsync()
        {
            var text = await _clipboard.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Clipboard does not contain text for signs.");

            ApplyPaste(text);
        }

        /// <summary>Used by unit tests with fixed paste strings.</summary>
        public void ApplyPaste(string clipboardText)
        {
            var pasted = new PastedRecordData(clipboardText);
            _lastPaste = pasted;
            _dataMap = new SignTemplateDataMap(SelectedTemplate, pasted);
            RebuildSignsFromMap();
            MappingUpdated?.Invoke(this, EventArgs.Empty);
        }

        public int SetColumnMapping(int columnIndex, int dropdownIndex)
        {
            if (_dataMap is null)
                return -1;

            int cleared = _dataMap.SetDropdownIndexForColumn(columnIndex, dropdownIndex);
            RebuildSignsFromMap();
            return cleared;
        }

        private void RebuildSignsFromMap()
        {
            Signs.Clear();
            if (_dataMap is null)
                return;

            foreach (var fields in _dataMap.CreateMappedRecords())
            {
                var data = new ChurchSign(_dataMap.Template)
                {
                    Fields = fields 
                };
                Signs.Add(data);
            }
            IsShowingPlaceholder = false;
            
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

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
