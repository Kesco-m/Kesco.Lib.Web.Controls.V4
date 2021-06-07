using System;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    [Serializable]
    public class TreeViewColumnUserFilter
    {
        public TreeViewColumnUserFilterEnum FilterType { get; set; }
        public object FilterValue1 { get; set; }
        public object FilterValue2 { get; set; }
    }
}