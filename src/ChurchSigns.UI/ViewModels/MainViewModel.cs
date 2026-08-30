using ChurchSigns.UI.Helpers;
using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Models;
using ChurchSigns.UI.Services;
using ChurchSigns.UI.Util;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ChurchSigns.UI.ViewModels
{
    public partial class MainViewModel(IClipboardService clipboard = null) : INotifyPropertyChanged
    {
        private readonly IClipboardService _clipboard = clipboard ?? new WindowsClipboardService();
        private SignTemplate _selectedTemplate = CreateBlankTemplate();
        private PastedRecordData _lastPaste;
        private SignTemplateDataMap _dataMap;

        public ObservableCollection<SignTemplate> Templates { get; } = [];
        public ObservableCollection<SignData> Signs { get; } = [];
        public ObservableCollection<GroupInfoList> GroupedTemplates { get; private set; }

        public SignTemplateDataMap DataMap => _dataMap;
        public PastedRecordData LastPaste => _lastPaste;

        public event EventHandler MappingReset;
        public event EventHandler MappingUpdated;
        public event PropertyChangedEventHandler PropertyChanged;

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

                // One placeholder sign so the preview shows the template
                Signs.Add(new SignData(_selectedTemplate));

                OnPropertyChanged();
                OnPropertyChanged(nameof(DataMap));
                MappingReset?.Invoke(this, EventArgs.Empty);
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
                var data = new SignData(_dataMap.Template)
                {
                    Fields = fields 
                };
                Signs.Add(data);
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

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
