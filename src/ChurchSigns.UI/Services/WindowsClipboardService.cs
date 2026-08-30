using ChurchSigns.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace ChurchSigns.UI.Services
{
    public sealed class WindowsClipboardService : IClipboardService
    {

        public async Task<string> GetTextAsync()
        {
            var view = Clipboard.GetContent();
            if (!view.Contains(StandardDataFormats.Text))
                return null;
            return await view.GetTextAsync();
        }

    }
}
