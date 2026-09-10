using System.Threading.Tasks;

namespace ChurchSigns.UI.Interfaces
{
    public interface IClipboardService
    {
        Task<string> GetTextAsync();
    }
}
