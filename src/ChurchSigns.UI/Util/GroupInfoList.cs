using System;
using System.Collections.Generic;
using System.Text;

namespace ChurchSigns.UI.Util
{
    /// <summary>
    /// Utility to support grouping in a ListView
    /// </summary>
    public class GroupInfoList(IEnumerable<object> items) : List<object>(items)
    {
        public object Key { get; set; }

        public override string ToString()
        {
            return Key.ToString();
        }
    }

}
