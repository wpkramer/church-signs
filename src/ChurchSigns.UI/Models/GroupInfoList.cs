
using System.Collections.Generic;
using System.Linq;
using ChurchSigns.UI.Models;

namespace ChurchSigns.UI.Models
{
    public partial class GroupInfoList : List<SignTemplate>
    {
        public GroupInfoList(string key, IEnumerable<SignTemplate> items) : base(items)
        {
            Key = key;
        }

        public string Key { get; set; }

        public override string ToString() => Key;
    }
}
