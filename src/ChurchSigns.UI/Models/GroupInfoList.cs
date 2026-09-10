
using System.Collections.Generic;

namespace ChurchSigns.UI.Models
{
    /// <summary>
    /// Use to group signs based on their category
    /// </summary>
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
