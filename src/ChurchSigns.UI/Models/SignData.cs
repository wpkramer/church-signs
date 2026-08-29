using ChurchSigns.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
// SignData.cs just a simple class
namespace ChurchSigns.UI.Models
{
    public class SignData : ISignData
    {
        private bool _hasChanged = false;
        private SignTemplate _template;
        private Dictionary<string, string> _fieldsValues;

        public Dictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();

        public SignData(SignTemplate template)
        {
            ArgumentNullException.ThrowIfNull(template, nameof(template));
            _template = template;
            _fieldsValues = new Dictionary<string, string>();

        }

      

        public bool AddFieldValue(string fieldName, string fieldValue)
        {
            if (_fieldsValues.ContainsKey(fieldName))
                return false;
            _fieldsValues[fieldName] = fieldValue;
            return true;
        }

        public string Title { get {  return _template.Title; } }
        public string Template { get { return _template.SvgSignTemplate; } }
        public bool HasChanged { get { return _hasChanged; } }

        public void TemplateWasUpated()
        {
           _hasChanged = true;
        }
    }
}
