using System;
using System.Collections.Generic;

namespace ChurchSigns.UI.Models
{
    /// <summary>
    /// Supports the synchronization of rendering _data XAML
    /// image when the template, data, or dimensions change
    /// </summary>
    internal class TemplatedImageSync
    {
        private string _template;
        private IDictionary<string, string> _data;
        private int _width;
        private int _height;

        public TemplatedImageSync()
        {
            _template = string.Empty;
            _data = new Dictionary<string, string>();
            _width = 0;
            _height = 0;
        }

        public TemplatedImageSync(string template, IDictionary<string, string> data, int width, int height)
        {
            if (template == null)
            {
                _template = string.Empty;
            }
            else
            {
                _template = template;
            }
            if (data == null)
            {
                _data = new Dictionary<string, string>();
            }
            else
            {
                _data = data;
            }

            _width = width;
            _height = height;
        }

        public void CopyFrom(TemplatedImageSync other)
        {
            ArgumentNullException.ThrowIfNull(other);

            _template = other._template;
            _data = new Dictionary<string, string>(other._data, StringComparer.OrdinalIgnoreCase);
            _width = other._width;
            _height = other._height;
        }

        public string Template { get => _template; }
        public IDictionary<string, string> Data { get => _data; }
        public int Width { get => _width; }
        public int Height { get => _height; }
        private bool DataEquals(IDictionary<string, string> other)
        {
            if (_data.Count != other.Count) return false;
            foreach (var kv in _data)
            {
                if (!other.TryGetValue(kv.Key, out var v)) return false;
                if (!string.Equals(kv.Value, v, StringComparison.Ordinal)) return false;
            }
            return true;
        }


        public bool Equals(TemplatedImageSync other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_width != other._width) return false;
            if (_height != other._height) return false;
            if (_template != other._template) return false;
  
            return DataEquals(other._data);
        }
    }
}
