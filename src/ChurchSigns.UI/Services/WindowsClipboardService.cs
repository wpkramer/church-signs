using ChurchSigns.UI.Interfaces;
using System;
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
                return string.Empty;
            return await view.GetTextAsync();
        }

    }
}
