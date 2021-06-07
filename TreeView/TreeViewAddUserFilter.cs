using System;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    [Serializable]
    public class TreeViewAddUserFilter
    {
        public string FilterId { get; set; }
        public string FilterName { get; set; }
        public string FilterSQL { get; set; }
    }
}