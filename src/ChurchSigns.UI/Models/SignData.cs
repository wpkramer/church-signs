using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.UI.Models
{
    public class SignData
    {
        public string Title { get; set; }
        public string Template { get; set; }
        public IDictionary<string,string> Fields { get; set; } = new Dictionary<string,string>();
        
    }
}
