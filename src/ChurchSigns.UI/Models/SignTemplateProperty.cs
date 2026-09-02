using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ChurchSigns.UI.Models
{
    public class SignTemplateProperty : INotifyPropertyChanged
    {
        public SignTemplateProperty(string name, string value)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(value, nameof(value));
            if(name.Length == 0)
            {
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            }
            Name = name;
            // don't set Value here, because it will trigger PropertyChanged event during construction
            _propertyValue = value;
        }

        public string Name { get; set; } = string.Empty;

        private string _propertyValue = string.Empty;
        public string Value
        {
            get { return _propertyValue; }
            set
            {
                if (_propertyValue != value)
                {
                    _propertyValue = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
