using ChurchSigns.UI.Interfaces;
using ChurchSigns.UI.Util;
using System;

using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace ChurchSigns.UI.Models
{
    public class SignTemplate
    {
        private bool _isvalid;
        private string _errorMessage;
        private List<SignData> _signList;
        private readonly TemplateStorageItem _templateStorageItem;
        public SignTemplate(TemplateStorageItem templateStorageItem)
        {
            try
            { 
                ArgumentNullException.ThrowIfNull(templateStorageItem);

                _templateStorageItem = templateStorageItem;
                _errorMessage = string.Empty;
                _signList = new List<SignData>();
                _isvalid = false;

                XmlDocument xmlDocument = new XmlDocument();
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
                    return $"Provided {_templateStorageItem.SignCategory} Signs";
                }
                return $"Custom {_templateStorageItem.SignCategory} Signs";
            } 
        }

        public IList<SignData> SignList { get { return _signList; } }

        public IReadOnlyList<string> FieldNames { get { return _templateStorageItem.FieldNames; } }

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

    }
}
