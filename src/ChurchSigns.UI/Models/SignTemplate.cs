using System;
using System.Collections.Generic;
using System.Xml;

namespace ChurchSigns.UI.Models
{
    public class SignTemplate
    {
        private const string DefaultColor = "#000000";
        private readonly bool _isvalid;
        // not sure if this will be used yet
        private readonly string _errorMessage;
        private readonly List<SignData> _signList;
        private readonly TemplateStorageItem _templateStorageItem;
        public SignTemplate(TemplateStorageItem templateStorageItem)
        {
            try
            { 
                ArgumentNullException.ThrowIfNull(templateStorageItem);

                _templateStorageItem = templateStorageItem;
                _errorMessage = string.Empty;
                _signList = [];
                _isvalid = false;

                XmlDocument xmlDocument = new();
                xmlDocument.LoadXml(templateStorageItem.Content);

                if (xmlDocument.DocumentElement != null)
                {
                    _isvalid = xmlDocument.DocumentElement.Name == "svg";
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"{ex.GetType().Name}: {ex.Message}";
                _isvalid = false;
            }

        }

        public string Group 
        { 
            get
            {
                if( _templateStorageItem.IsProvided)
                {
                    return $"{_templateStorageItem.SignCategory} Signs";
                }
                return $"Your {_templateStorageItem.SignCategory} Signs";
            } 
        }

        public SignCategory Category { get => _templateStorageItem.SignCategory; }
        public string Filename { get => _templateStorageItem.Filename; }

        public bool IsProvided { get { return _templateStorageItem.IsProvided; } }
        public bool IsCustom { get { return !_templateStorageItem.IsProvided; } }

        public IReadOnlyList<string> FieldNames { get { return _templateStorageItem.FieldNames; } }

        public Dictionary<string, string> PreviewFields
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (string fieldname in _templateStorageItem.FieldNames)
                {
                    if(_templateStorageItem.PreviewFields.Fields.TryGetValue(fieldname, out string value))
                    {
                        result.TryAdd(fieldname, value);
                    }
                    else if (fieldname.Contains("color", StringComparison.OrdinalIgnoreCase))
                    {
                        result.TryAdd(fieldname, DefaultColor);
                    }
                    else if(fieldname.Contains("colour", StringComparison.OrdinalIgnoreCase))
                    {
                        result.TryAdd(fieldname, DefaultColor);
                    }
                    else
                    {
                        result.TryAdd(fieldname, string.Empty);
                    }
                }
                return result;
            }
        }

        public bool IsValid { get { return _isvalid; } }

        public string SvgSignTemplate
        {
            get
            {
                if (!_isvalid)
                    return string.Empty;
                return _templateStorageItem.Content;
            }
        }

        public string Title 
        {
            get 
            { 
                return _templateStorageItem.DisplayName; 
            }
        }

        public TemplateStorageItem ToStorageItem()
        {
            return _templateStorageItem;
        }

        internal void UpdatePreviewFields(SignTemplateProperty[] signTemplateProperties)
        {
            _templateStorageItem.PreviewFields.Fields.Clear();
            foreach (var signTemplateProperty in signTemplateProperties)
            {
                _templateStorageItem.PreviewFields.Fields[signTemplateProperty.Name] = signTemplateProperty.Value;
            }
        }
    }
}
