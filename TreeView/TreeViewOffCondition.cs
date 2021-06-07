using System;

namespace Kesco.Lib.Web.Controls.V4.TreeView
{
    [Serializable]
    public class TreeViewOffCondition
    {
        public TreeViewOffConditionTypeEnum ConditionType { get; set; }
        public string FieldName1 { get; set; }
        public string FieldName2 { get; set; }
        public bool? Condition { get; set; }
    }
}