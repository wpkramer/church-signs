using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ChurchSigns.UI.Models;

namespace ChurchSigns.UI.ViewModels
{
    public partial class SignTemplateViewModel : INotifyPropertyChanged
    {
        private readonly SignTemplate _signTemplate;

        public ObservableCollection<SignTemplateProperty> PreviewFields { get; }
        public SignTemplateViewModel(SignTemplate signTemplate)
        {
            ArgumentNullException.ThrowIfNull(signTemplate, nameof(signTemplate));
            _signTemplate = signTemplate;
            PreviewFields = new ObservableCollection<SignTemplateProperty>();
            foreach (var dictItem in _signTemplate.PreviewFields)
            {
                PreviewFields.Add(new SignTemplateProperty(dictItem.Key, dictItem.Value));
            }
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public void UpdateTemplate()
        {
            _signTemplate.UpdatePreviewFields(PreviewFields.ToArray());

        }

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}


    }
}
